using Godot;

namespace LifeGame;

// The water's look (atmosphere-31, Justin: "the ripple animation is horrible. Can we get
// 3d water/more interactive water with reflections?"). Stylised, not photoreal:
// - near the player, a dense surface with gentle waves moving its vertices, plus the
//   ripples of the interactive wave simulation (WaterRipples) where things disturb it;
// - the bed seen through the shallows, bent a little by the waves, deepening in soft
//   flat painted bands (aqua shelf, teal, soft blue-teal deep; env-38, Justin: "cuter
//   and match the style more"), a soft foam band at the bank with a thin line lapping
//   out from it, and pale painted ripple lines where the wind's ripples crest;
// - a light, blurred hint of what is on screen (trees, the player, the far shore),
//   falling back to the sky's own colours, in two soft steps at a glance;
// - no sun glare: only a soft warm sheen on the ripples facing the sun;
// - a few leaves floating near the banks, drifting with the wind: many in autumn,
//   turned gold and red, a stray green one in summer, none under winter ice;
// - streams that run: their ripples, leaves and a few pale streaks of foam slide
//   downstream along the channel (a flow map round the player, AtmosphereWaterSurface).
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
        ReflectSteps = "water_reflect_steps", Sim = "water_sim", SimRect = "water_sim_rect",
        Flow = "water_flow", FlowRect = "water_flow_rect";

    // The near mesh reaches this far from the player; the far plane takes over beyond.
    public const float NearRadius = 44f;

    static bool globals;

    static void EnsureGlobals()
    {
        if (globals) return;
        globals = true;
        // Added once per run (the flag above); never list the globals to check, as
        // GlobalShaderParameterGetList is editor-only and slow.
        static void Add(StringName name, RenderingServer.GlobalShaderParameterType type, Variant value) =>
            RenderingServer.GlobalShaderParameterAdd(name, type, value);
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
        var still = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        still.Fill(new Color(0.5f, 0.5f, 0f, 1f));
        Add(Flow, RenderingServer.GlobalShaderParameterType.Sampler2D, ImageTexture.CreateFromImage(still));
        Add(FlowRect, RenderingServer.GlobalShaderParameterType.Vec4, new Vector4(0f, 0f, 1f, 0f));
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
        // Painted tints (env-38): a pale aqua shelf, a teal middle and a soft blue-teal
        // deep, laid in flat bands by how deep the water is.
        uniform vec4 shelf_color : source_color = vec4(0.56, 0.80, 0.72, 1.0);
        uniform vec4 mid_color : source_color = vec4(0.30, 0.60, 0.60, 1.0);
        uniform vec4 deep_color : source_color = vec4(0.15, 0.36, 0.46, 1.0);
        uniform vec4 shallow_color : source_color = vec4(0.42, 0.66, 0.58, 1.0);
        uniform vec4 foam_color : source_color = vec4(0.96, 0.97, 0.93, 1.0);
        // A trough's water is only a hand deep: it reads its depth this many times
        // over and has no lapping line (ClayLook.TroughWater).
        uniform float depth_scale = 1.0;
        uniform float shore_lap = 1.0;
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
        global uniform sampler2D water_flow : filter_linear, repeat_disable;
        global uniform vec4 water_flow_rect;
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

        // Which way and how fast the water runs here (metres a second), 0 in ponds.
        vec2 flow_at(vec2 p) {
            if (water_flow_rect.w < 0.5) return vec2(0.0);
            vec2 uv = (p - water_flow_rect.xy) / water_flow_rect.z;
            if (uv.x <= 0.0 || uv.y <= 0.0 || uv.x >= 1.0 || uv.y >= 1.0) return vec2(0.0);
            return (textureLod(water_flow, uv, 0.0).rg - 0.5) * 2.0 * 0.7;
        }

        // The phase of the flow's two overlapping cycles, and how much the second shows:
        // each cycle slides the pattern downstream and is swapped out before it stretches.
        vec3 flow_phase(float t) {
            float a = fract(t * 0.3), b = fract(t * 0.3 + 0.5);
            return vec3(a, b, abs(1.0 - 2.0 * a));
        }

        // The fine ripples, carried downstream where the water runs.
        float fine_ripples(vec2 p, float t, vec2 f) {
            if (dot(f, f) < 1e-5) return ripples(p, t);
            vec3 ph = flow_phase(t);
            float r0 = ripples(p - f * ph.x * 3.3, t);
            float r1 = ripples(p - f * ph.y * 3.3 + vec2(0.37, 0.71), t);
            return mix(r1, r0, ph.z);
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
            vec2 f = flow_at(p);
            // Running water is a little rougher than a still pond.
            float fine = (0.012 + 0.03 * wind_strength) * (1.0 + 1.5 * length(f));
            float h0 = swell(p, t) * amp + fine_ripples(p, t, f) * fine + sim(p);
            vec2 slope = vec2(
                swell(p + vec2(e, 0.0), t) * amp + fine_ripples(p + vec2(e, 0.0), t, f) * fine + sim(p + vec2(e, 0.0)) - h0,
                swell(p + vec2(0.0, e), t) * amp + fine_ripples(p + vec2(0.0, e), t, f) * fine + sim(p + vec2(0.0, e)) - h0) / e;
            float rain = smoothstep(0.02, 0.6, rain_amount);
            if (rain > 0.0) slope += rings(p, t, mix(0.15, 0.9, rain)) * 0.1 * rain;
            vec3 n = normalize(vec3(-slope.x, 1.0, -slope.y));
            vec3 nv = normalize((VIEW_MATRIX * vec4(n, 0.0)).xyz);

            // How deep the water is here: straight down from the surface to whatever is
            // behind it (the bed, or a wader's legs). Measured along the view ray, as it
            // used to be, the shore foam and the colour bands grew and shrank as the
            // camera turned and haloed anything standing in the water (env-38).
            float d0 = textureLod(depth_tex, SCREEN_UV, 0.0).r;
            vec4 b0 = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, d0, 1.0);
            b0.xyz /= b0.w;
            float depth0 = clamp(world_pos.y - (INV_VIEW_MATRIX * vec4(b0.xyz, 1.0)).y, 0.0, 20.0);

            // The bed, bent a little by the waves; never pulling in anything in front.
            vec2 ruv = SCREEN_UV + n.xz * 0.025 * clamp(depth0, 0.0, 1.0);
            float d1 = textureLod(depth_tex, ruv, 0.0).r;
            vec4 b1 = INV_PROJECTION_MATRIX * vec4(ruv * 2.0 - 1.0, d1, 1.0);
            b1.xyz /= b1.w;
            if (b1.z > VERTEX.z) { ruv = SCREEN_UV; b1 = b0; }
            vec3 bed = textureLod(screen, ruv, 0.0).rgb;
            float depth = clamp(world_pos.y - (INV_VIEW_MATRIX * vec4(b1.xyz, 1.0)).y, 0.0, 20.0);

            // Flat painted bands by depth, with soft short blends between them and a
            // gentle wobble so the edges are hand-drawn rather than ruled.
            float wob = texture(noise, p * 0.18 + vec2(t * 0.015, t * 0.01)).r;
            // Streams' beds lie 0.22 m down and ponds' 0.4 m, so a stream reads teal
            // with a pale shelf at its banks and a pond deepens to its blue-teal middle.
            float dk = depth * depth_scale * (0.9 + 0.2 * wob);
            vec3 lit = water_ambient + water_sun_color * 0.25;
            vec3 col = mix(shelf_color.rgb, mid_color.rgb, smoothstep(0.1, 0.16, dk));
            col = mix(col, deep_color.rgb, smoothstep(0.26, 0.32, dk));
            col *= lit;
            // The bed shows through only on the shelf, tinted by the water.
            float clear = 1.0 - smoothstep(0.03, 0.15, dk);
            col = mix(col, bed * shallow_color.rgb * 1.4, clear * 0.55);

            // Reflections: a soft, blurred hint of what stands above the water (traced
            // along the screen), falling back to the sky's own colours. Kept light so
            // the water stays painted rather than mirror-like.
            vec3 rv = reflect(-VIEW, nv);
            vec3 rw = normalize((INV_VIEW_MATRIX * vec4(rv, 0.0)).xyz);
            vec3 refl = mix(water_sky_horizon, water_sky_top, smoothstep(0.15, 0.45, rw.y));
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
                    // A hit is something just in front of the ray, not the bank far
                    // behind it (the old, looser test smeared grass across the water).
                    if (sp.z > pos.z && sp.z - pos.z < len * 1.2 + 0.15) {
                        float border = smoothstep(0.0, 0.15, min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                        refl = mix(refl, textureLod(screen, uv, 3.0).rgb, border * 0.8);
                        break;
                    }
                }
            }
            float facing = clamp(dot(nv, VIEW), 0.0, 1.0);
            // Two soft steps instead of a smooth mirror ramp.
            float glance = 1.0 - facing;
            float fres = 0.08 + 0.14 * smoothstep(0.55, 0.65, glance) + 0.16 * smoothstep(0.8, 0.88, glance);
            col = mix(col, refl, fres);

            // Painted ripple lines: thin pale strokes where the wind's ripples crest,
            // fading with distance; no sun glare, only a soft warm sheen on the
            // ripples that face the sun.
            float rp = fine_ripples(p * 0.8, t * 0.8, f);
            float line = smoothstep(0.62, 0.7, rp) * (1.0 - smoothstep(0.78, 0.9, rp));
            line *= (0.25 + 0.35 * wind_strength) * (1.0 - smoothstep(12.0, 30.0, reach)) * smoothstep(0.08, 0.25, depth);
            vec3 sunw = normalize(water_sun_dir);
            float sheen = smoothstep(0.85, 0.9, dot(rw, sunw)) * smoothstep(0.1, 0.4, sunw.y);
            col = mix(col, foam_color.rgb * lit * 1.05, line * 0.5);
            col += water_sun_color * sheen * 0.06;

            // Shore foam: a soft band hugging the bank, and a thin second line that
            // laps slowly out from it and fades, like a gentle tide; foam on high
            // simulated crests too.
            float foam = 1.0 - smoothstep(0.035, 0.06 + 0.05 * wob, depth * depth_scale);
            float lap = fract(t * 0.18 + wob * 0.6);
            // It stays on the bank's slope, shallower than any stream or pond bed.
            float lap_at = 0.07 + 0.08 * lap;
            foam = max(foam, (1.0 - smoothstep(0.0, 0.012, abs(depth - lap_at))) * (1.0 - lap) * 0.7 * shore_lap);
            foam = max(foam, smoothstep(0.05, 0.09, abs(sim(p))) * 0.5);
            // Pale streaks of foam slide down a running stream.
            float fl = length(f);
            if (fl > 0.02) {
                vec3 ph = flow_phase(t);
                vec2 across = vec2(-f.y, f.x) / fl;
                vec2 sp = vec2(dot(p, across) * 2.2, dot(p, f / fl) * 0.35);
                float s0 = texture(noise, (sp - vec2(0.0, ph.x * 3.3 * fl * 0.35)) * 0.15).r;
                float s1 = texture(noise, (sp - vec2(0.0, ph.y * 3.3 * fl * 0.35)) * 0.15 + 0.5).r;
                foam = max(foam, smoothstep(0.66, 0.74, mix(s1, s0, ph.z)) * clamp(fl * 0.6, 0.0, 0.35));
            }
            col = mix(col, foam_color.rgb * (water_ambient + water_sun_color * 0.5), foam * 0.8);

            // Leaves near the bank, most in autumn, none once it freezes.
            float leafy = (0.03 + 0.3 * season_autumn) * (1.0 - smoothstep(0.12, 0.3, depth0)) * smoothstep(-0.9, -0.6, season_warmth);
            if (leafy > 0.005) {
                vec4 lf;
                if (dot(f, f) < 1e-5) lf = leaf(p, t, leafy);
                else {
                    // In a stream the leaves ride the current.
                    vec3 ph = flow_phase(t);
                    vec4 l0 = leaf(p - f * ph.x * 3.3, t, leafy);
                    vec4 l1 = leaf(p - f * ph.y * 3.3 + vec2(3.7, 1.3), t, leafy);
                    lf = mix(l1, l0, ph.z);
                }
                col = mix(col, lf.rgb * (water_ambient + water_sun_color * 0.6), lf.a * (1.0 - foam));
            }

            ALBEDO = col;
            ALPHA = smoothstep(0.0, 0.02, depth0);

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
