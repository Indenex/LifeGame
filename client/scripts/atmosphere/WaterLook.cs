using Godot;

namespace LifeGame;

// The water's look (atmosphere-31, Justin: "the ripple animation is horrible. Can we get
// 3d water/more interactive water with reflections?"). Stylised, not photoreal:
// - near the player, a dense surface with gentle waves moving its vertices, plus the
//   ripples of the interactive wave simulation (WaterRipples) where things disturb it;
// - the bed seen through the shallows, bent a little by the waves, deepening in soft
//   painted bands to a dark teal, with a wavering foam line where it meets the bank;
// - reflections of what is on screen (trees, the player, the far shore) traced along
//   the screen, softened, falling back to the sky's own colours, stronger at a glance;
// - the sun's glint as a crisp painted shape rather than a smeared highlight;
// - a few leaves floating near the banks, drifting with the wind: many in autumn,
//   turned gold and red, a stray green one in summer, none under winter ice.
// The far plane uses the same shader without the waves and gives way to the near mesh
// inside near_radius, so the two never overlap.
public static class WaterLook
{
    // Globals Atmosphere sets each frame: the sky at the top and at the horizon, the
    // sun or moon (direction toward it, colour times strength), the ambient light,
    // how many steps the screen reflections may take (0 off), and the wave
    // simulation's texture and rectangle (x, z of its corner, size in metres, 1 when on).
    public static readonly StringName SkyTop = "water_sky_top", SkyHorizon = "water_sky_horizon",
        SunDir = "water_sun_dir", SunColor = "water_sun_color", Ambient = "water_ambient",
        ReflectSteps = "water_reflect_steps", Sim = "water_sim", SimRect = "water_sim_rect";

    // The near mesh reaches this far from the player; the far plane takes over beyond.
    public const float NearRadius = 44f;

    static bool globals;

    static void EnsureGlobals()
    {
        if (globals) return;
        globals = true;
        var have = RenderingServer.GlobalShaderParameterGetList();
        void Add(StringName name, RenderingServer.GlobalShaderParameterType type, Variant value)
        {
            if (!have.Contains(name)) RenderingServer.GlobalShaderParameterAdd(name, type, value);
        }
        Add(SkyTop, RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(0.35f, 0.55f, 0.8f));
        Add(SkyHorizon, RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(0.7f, 0.78f, 0.82f));
        Add(SunDir, RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(0.3f, 0.8f, 0.5f).Normalized());
        Add(SunColor, RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(2f, 1.8f, 1.5f));
        Add(Ambient, RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(0.4f, 0.45f, 0.5f));
        Add(ReflectSteps, RenderingServer.GlobalShaderParameterType.Float, 16f);
        var blank = Image.CreateEmpty(1, 1, false, Image.Format.Rgbah);
        blank.Fill(new Color(0.5f, 0.5f, 0f, 1f));
        Add(Sim, RenderingServer.GlobalShaderParameterType.Sampler2D, ImageTexture.CreateFromImage(blank));
        Add(SimRect, RenderingServer.GlobalShaderParameterType.Vec4, new Vector4(0f, 0f, 1f, 0f));
    }

    // Look.EnsureWeatherGlobals must have run first (Look.WaterSurface does that).
    public static ShaderMaterial Make(bool near)
    {
        EnsureGlobals();
        var material = new ShaderMaterial { Shader = shader ??= new Shader { Code = Code } };
        material.SetShaderParameter("is_near", near);
        material.SetShaderParameter("near_radius", NearRadius);
        material.SetShaderParameter("noise", Look.Clouds);
        return material;
    }

    static Shader? shader;

    const string Code = """
        shader_type spatial;
        render_mode blend_mix, depth_draw_always, cull_disabled, unshaded, shadows_disabled;

        uniform bool is_near = false;
        uniform float near_radius = 44.0;
        uniform vec4 deep_color : source_color = vec4(0.05, 0.19, 0.21, 1.0);
        uniform vec4 shallow_color : source_color = vec4(0.42, 0.66, 0.58, 1.0);
        uniform vec4 foam_color : source_color = vec4(0.93, 0.95, 0.92, 1.0);
        uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
        uniform sampler2D depth_tex : hint_depth_texture, filter_nearest;
        uniform sampler2D noise : repeat_enable, filter_linear;

        global uniform vec3 player_position;
        global uniform float wind_strength;
        global uniform float wind_gust;
        global uniform vec2 wind_dir;
        global uniform float rain_amount;
        global uniform float far_land_radius;
        global uniform vec3 fog_tint;
        global uniform vec3 water_sky_top;
        global uniform vec3 water_sky_horizon;
        global uniform vec3 water_sun_dir;
        global uniform vec3 water_sun_color;
        global uniform vec3 water_ambient;
        global uniform float water_reflect_steps;
        global uniform sampler2D water_sim : filter_linear, repeat_disable;
        global uniform vec4 water_sim_rect;
        global uniform float season_autumn;
        global uniform float season_warmth;

        varying vec3 world_pos;
        varying float wave_fade;

        float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

        // Gentle swell: a few long soft waves crossing, leaning with the wind.
        float swell(vec2 p, float t) {
            vec2 w = normalize(wind_dir + vec2(0.001));
            vec2 s = vec2(-w.y, w.x);
            return sin(dot(p, w) * 0.9 + t * 1.2) * 0.5
                 + sin(dot(p, normalize(w + s * 0.7)) * 1.6 + t * 1.7) * 0.3
                 + sin(dot(p, normalize(w - s * 0.9)) * 2.7 + t * 2.4) * 0.2;
        }

        // Fine wind ripples on top, only in the shading.
        float ripples(vec2 p, float t) {
            return sin(dot(p, vec2(2.3, 1.1)) * 2.1 + t * 2.6) * 0.5
                 + sin(dot(p, vec2(-1.2, 2.4)) * 3.3 + t * 3.1) * 0.3
                 + sin(dot(p, vec2(1.9, -2.2)) * 5.1 + t * 3.9) * 0.2;
        }

        // Height of the interactive ripples (WaterRipples), in metres, 0 outside it.
        float sim(vec2 p) {
            if (water_sim_rect.w < 0.5) return 0.0;
            vec2 uv = (p - water_sim_rect.xy) / water_sim_rect.z;
            if (uv.x <= 0.0 || uv.y <= 0.0 || uv.x >= 1.0 || uv.y >= 1.0) return 0.0;
            float edge = smoothstep(0.0, 0.08, min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
            return (textureLod(water_sim, uv, 0.0).r - 0.5) * 0.25 * edge;
        }

        // Rain rings for the water beyond the simulation.
        vec2 rings(vec2 p, float t, float density) {
            vec2 slope = vec2(0.0);
            vec2 q = p * 1.3;
            vec2 cell = floor(q);
            for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++) {
                vec2 c = cell + vec2(float(dx), float(dz));
                float h = hash(c);
                if (h > density) continue;
                float rate = 0.7 + h * 0.6;
                float age = fract(t * rate + h * 7.0);
                float drop = floor(t * rate + h * 7.0);
                vec2 center = c + 0.1 + vec2(hash(c + drop * 1.37 + 3.1), hash(c + drop * 2.11 + 5.7)) * 0.8;
                vec2 d = q - center;
                float r = length(d);
                float band = r - age * 0.55;
                float wave = sin(band * 40.0) * exp(-band * band * 260.0) * (1.0 - age) * (1.0 - age);
                slope += (r > 0.001 ? d / r : vec2(0.0)) * wave;
            }
            return slope;
        }

        // A floating leaf, if one drifts over this point: its colour and how much of
        // it covers the point (0 none). Leaves sit in cells of a slowly drifting grid,
        // each with its own turn and shade.
        vec4 leaf(vec2 p, float t, float density) {
            vec2 drift = wind_dir * t * 0.06 + vec2(sin(t * 0.07), cos(t * 0.05)) * 0.4;
            vec2 q = (p - drift) / 0.7;
            vec2 cell = floor(q);
            float h = hash(cell + 11.3);
            if (h > density) return vec4(0.0);
            vec2 centre = cell + 0.25 + 0.5 * vec2(hash(cell + 2.7), hash(cell + 8.1));
            float a = hash(cell + 5.9) * 6.2832 + sin(t * 0.3 + h * 20.0) * 0.4;
            vec2 d = mat2(vec2(cos(a), sin(a)), vec2(-sin(a), cos(a))) * (q - centre);
            // A pointed oval about 9 cm long, with a faint darker vein.
            float shape = length(vec2(d.x * 7.5, d.y * 13.0 * (1.0 + 0.6 * d.x * 7.5)));
            float cover = 1.0 - smoothstep(0.85, 1.0, shape);
            if (cover <= 0.0) return vec4(0.0);
            float turned = clamp(season_autumn * 1.4 + (hash(cell + 1.3) - 0.5) * 0.4, 0.0, 1.0);
            vec3 green = vec3(0.20, 0.32, 0.10);
            vec3 gold = mix(vec3(0.62, 0.40, 0.07), vec3(0.55, 0.15, 0.05), hash(cell + 3.3));
            vec3 colour = mix(green, mix(gold, vec3(0.32, 0.20, 0.10), smoothstep(0.75, 1.0, season_autumn)), turned);
            colour *= 1.0 - 0.25 * (1.0 - smoothstep(0.0, 0.04, abs(d.y)));
            return vec4(colour, cover);
        }

        void vertex() {
            vec3 w = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            wave_fade = 0.0;
            if (is_near) {
                float r = length(w.xz - player_position.xz);
                wave_fade = 1.0 - smoothstep(near_radius * 0.75, near_radius * 0.95, r);
                float amp = 0.02 + 0.05 * wind_strength * (0.6 + 0.6 * wind_gust);
                float dy = (swell(w.xz, TIME) * amp + sim(w.xz)) * wave_fade;
                VERTEX.y += dy;
                w.y += dy;
            }
            world_pos = w;
        }

        void fragment() {
            vec2 p = world_pos.xz;
            float t = TIME;
            float reach = length(p - player_position.xz);
            if (is_near ? reach > near_radius : reach < near_radius) discard;

            // The surface's slope: swell (near only), fine ripples, the simulation
            // and the rain.
            float e = 0.08;
            float amp = (0.02 + 0.05 * wind_strength * (0.6 + 0.6 * wind_gust)) * wave_fade;
            float fine = 0.012 + 0.03 * wind_strength;
            float h0 = swell(p, t) * amp + ripples(p, t) * fine + sim(p);
            vec2 slope = vec2(
                swell(p + vec2(e, 0.0), t) * amp + ripples(p + vec2(e, 0.0), t) * fine + sim(p + vec2(e, 0.0)) - h0,
                swell(p + vec2(0.0, e), t) * amp + ripples(p + vec2(0.0, e), t) * fine + sim(p + vec2(0.0, e)) - h0) / e;
            float rain = smoothstep(0.02, 0.6, rain_amount);
            if (rain > 0.0) slope += rings(p, t, mix(0.15, 0.9, rain)) * 0.1 * rain;
            vec3 n = normalize(vec3(-slope.x, 1.0, -slope.y));
            vec3 nv = normalize((VIEW_MATRIX * vec4(n, 0.0)).xyz);

            // How much water lies between the surface and the bed behind it.
            float d0 = textureLod(depth_tex, SCREEN_UV, 0.0).r;
            vec4 b0 = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, d0, 1.0);
            b0.xyz /= b0.w;
            float through0 = clamp(VERTEX.z - b0.z, 0.0, 20.0);

            // The bed, bent a little by the waves; never pulling in anything in front.
            vec2 ruv = SCREEN_UV + n.xz * 0.035 * clamp(through0, 0.0, 1.0);
            float d1 = textureLod(depth_tex, ruv, 0.0).r;
            vec4 b1 = INV_PROJECTION_MATRIX * vec4(ruv * 2.0 - 1.0, d1, 1.0);
            b1.xyz /= b1.w;
            if (b1.z > VERTEX.z) { ruv = SCREEN_UV; b1 = b0; }
            float through = clamp(VERTEX.z - b1.z, 0.0, 20.0);
            vec3 bed = textureLod(screen, ruv, 0.0).rgb;

            // Deeper reads darker, in soft painted bands.
            float k = clamp(through / 2.2, 0.0, 1.0);
            float bands = smoothstep(0.02, 0.1, k) * 0.34 + smoothstep(0.3, 0.38, k) * 0.33 + smoothstep(0.66, 0.74, k) * 0.33;
            k = mix(k, bands, 0.7);
            vec3 lit = water_ambient + water_sun_color * 0.2;
            vec3 deep = deep_color.rgb * lit;
            vec3 col = mix(bed * shallow_color.rgb * 1.5, deep, k);

            // Reflections: march along the screen for what stands above the water;
            // where nothing is found, the sky.
            vec3 rv = reflect(-VIEW, nv);
            vec3 rw = normalize((INV_VIEW_MATRIX * vec4(rv, 0.0)).xyz);
            vec3 refl = mix(water_sky_horizon, water_sky_top, pow(clamp(rw.y, 0.0, 1.0), 0.5));
            int steps = int(water_reflect_steps);
            if (steps > 0) {
                vec3 pos = VERTEX;
                float len = 0.35;
                for (int i = 0; i < 48; i++) {
                    if (i >= steps) break;
                    pos += rv * len;
                    len *= 1.3;
                    vec4 c = PROJECTION_MATRIX * vec4(pos, 1.0);
                    vec2 uv = c.xy / c.w * 0.5 + 0.5;
                    if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0) break;
                    float sd = textureLod(depth_tex, uv, 0.0).r;
                    vec4 sp = INV_PROJECTION_MATRIX * vec4(uv * 2.0 - 1.0, sd, 1.0);
                    sp.xyz /= sp.w;
                    if (sp.z > pos.z && sp.z - pos.z < len * 2.0 + 0.4) {
                        float border = smoothstep(0.0, 0.12, min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                        refl = mix(refl, textureLod(screen, uv, 1.5).rgb, border);
                        break;
                    }
                }
            }
            float facing = clamp(dot(nv, VIEW), 0.0, 1.0);
            float fres = 0.12 + 0.68 * pow(1.0 - facing, 4.0);
            col = mix(col, refl, fres);

            // The sun's glint: a crisp painted shape, with a soft halo round it.
            float s = max(dot(rw, normalize(water_sun_dir)), 0.0);
            col += water_sun_color * (smoothstep(0.988, 0.992, s) * 0.9 + smoothstep(0.94, 0.97, s) * 0.08);

            // A wavering foam line along the bank, and foam on high simulated crests.
            float wob = texture(noise, p * 0.18 + vec2(t * 0.015, t * 0.01)).r;
            float foam = 1.0 - smoothstep(0.03, 0.1 + 0.1 * wob, through);
            foam = max(foam, smoothstep(0.035, 0.07, abs(sim(p))) * 0.6);
            col = mix(col, foam_color.rgb * (water_ambient + water_sun_color * 0.5), foam * 0.75);

            // Leaves near the bank, most in autumn, none once it freezes.
            float leafy = (0.03 + 0.3 * season_autumn) * (1.0 - smoothstep(0.5, 4.0, through0)) * smoothstep(-0.9, -0.6, season_warmth);
            if (leafy > 0.005) {
                vec4 lf = leaf(p, t, leafy);
                col = mix(col, lf.rgb * (water_ambient + water_sun_color * 0.6), lf.a * (1.0 - foam));
            }

            ALBEDO = col;
            ALPHA = smoothstep(0.0, 0.03, through0);

            // Like the land, the water melts into the fog at the edge of the far land.
            if (far_land_radius > 0.0) {
                if (reach > far_land_radius) discard;
                float rim = smoothstep(0.75 * far_land_radius, far_land_radius, reach);
                ALBEDO = mix(ALBEDO, fog_tint, rim);
                ALPHA = mix(ALPHA, 1.0, rim);
            }
        }
        """;
}
