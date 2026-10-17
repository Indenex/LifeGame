shader_type spatial;
#define PAINTED_FOLIAGE
#define GHOST
render_mode cull_disabled, depth_draw_always;
global uniform float wetness;
global uniform float wind_strength;
// The gust heard in the wind right now (0 to 1), so what moves matches the sound.
global uniform float wind_gust;
// Which way the wind blows across the ground (unit x, z); clouds drift this way.
global uniform vec2 wind_dir;
global uniform float rain_amount;
global uniform float cloud_cover;
global uniform vec2 cloud_offset;
global uniform float world_seconds;
// How far the distant land reaches round the player (0 when off), and the
// colour of the fog it melts into at its rim.
global uniform float far_land_radius;
global uniform vec3 fog_tint;
// How far up the player is wet from wading, and how wet (0 dry to 1 just out).
global uniform float wet_line;
global uniform float wet_amount;
// The season (Atmosphere, D-099): warmth -1 deep winter to 1 high summer,
// autumn 0 green to 1 leaves fully turned, snow lying on the ground and
// snow falling now, all 0 to 1.
global uniform float season_warmth;
global uniform float season_autumn;
global uniform float snow_cover;
global uniform float snowing;
// How far through the year it is: 0 spring, 0.25 summer, 0.5 autumn, 0.75 winter.
global uniform float season_year;
// How much standing water there is after rain, 0 dry to 1 puddled; it rises
// once the ground is soaked and lingers after the rain stops.
global uniform float puddles;
const vec3 SNOW = vec3(0.86, 0.89, 0.94);

// How bare broadleaf trees are: bare through winter, leafing out again early
// in spring.
float leaf_drop() {
    return (1.0 - smoothstep(-0.85, -0.35, season_warmth)) * step(0.3, season_autumn);
}
uniform sampler2D clouds : repeat_enable, filter_linear_mipmap;

float cloud_shadow(vec3 p) {
    if (cloud_cover <= 0.01) return 1.0;
    // Clouds a few dozen meters across, so on a fair day their shadows
    // slide over the land in patches; under full overcast they merge.
    float n = texture(clouds, (p.xz + cloud_offset) * 0.01).r;
    float edge = mix(0.68, 0.2, cloud_cover);
    return 1.0 - smoothstep(edge - 0.1, edge + 0.12, n) * 0.5;
}

uniform vec4 albedo : source_color = vec4(1.0);
uniform sampler2D brush : repeat_enable, filter_linear;
uniform sampler2D detail : repeat_enable, filter_linear_mipmap;
uniform float sway = 0.0;
uniform float sway_height = 1.0;
uniform float brush_scale = 0.09;
uniform float brush_strength = 0.14;
uniform float detail_scale = 1.0;
uniform vec3 detail_stretch = vec3(1.0);
uniform float bump = 1.0;
uniform float cavity = 0.4;
uniform bool see_through = false;
// Set on a ghost (never a solid) to fade it by hand, 0 solid to 1 clear, in place of the
// tree test: buildings drive it while the player is under their roof.
uniform float fade_override = -1.0;
// A building ghost cuts away like Project Zomboid (D-168). cut_mode 1 is a wall:
// it fades on the side facing the camera (cam_dir, flat, from the building's
// centre cut_origin to the camera) and stays solid on the far side. cut_mode 2
// is a roof: it clears as fade_override reaches 1, entirely or only round the player
// (cut_floor). 0 is the trees.
uniform float cut_mode = 0.0;
uniform vec3 cut_origin = vec3(0.0);
uniform vec2 cam_dir = vec2(0.0, 1.0);
// How much of a wall facing the camera (or of a roof) fades away from the player's
// circle: nearly all from inside, less (a wall) or none (a roof) from outside.
uniform float cut_floor = 0.85;
// 1 on the ground material only: where puddles may gather after rain.
uniform float puddles_ok = 0.0;
global uniform vec3 player_position;
global uniform vec3 view_camera_position;

// How far a tree has faded because it hides the player: 0 solid, 1 fully in
// the way. The crown is taken as an upright capsule round the canopy, sized
// by the tree's own scale, and the tree fades when the line of sight from
// the camera to the player passes through it anywhere, including right at
// the player, so a crown overhead fades too (Justin's playtest). Trunk and
// canopy share a transform, so the whole tree eases in and out together.
// Only the main camera fades things, so shadows stay whole.
float fade_amount(mat4 model, vec3 cam) {
    if (distance(cam, view_camera_position) > 0.5) return 0.0;
    float tall = length(model[1].xyz);
    float wide = length(model[0].xyz);
    vec3 base = model[3].xyz;
    vec3 p1 = cam, d1 = player_position + vec3(0.0, 1.0, 0.0) - cam;
    vec3 p2 = base + vec3(0.0, 1.6 * tall, 0.0), d2 = vec3(0.0, 2.8 * tall, 0.0);
    vec3 r = p1 - p2;
    float a = dot(d1, d1), b = dot(d1, d2), c = dot(d1, r), e = dot(d2, d2), f = dot(d2, r);
    float denom = a * e - b * b;
    float s = denom > 1e-5 ? clamp((b * f - c * e) / denom, 0.0, 1.0) : 0.0;
    float t = clamp((b * s + f) / e, 0.0, 1.0);
    s = clamp((b * t - c) / a, 0.0, 1.0);
    float d = distance(p1 + d1 * s, p2 + d2 * t);
    // A canopy reaches about 1.9 times the tree's width from its axis; fade
    // fully inside that and ease out a little beyond.
    return 1.0 - smoothstep(1.9 * wide, 2.7 * wide, d);
}

varying vec3 tint;
varying float fade;
varying vec3 world_pos;
varying vec3 world_normal;
varying float shade;
varying float wet;
// Environment's season code per instance (INSTANCE_CUSTOM.w): 1 broadleaf
// canopy, 2 flower, 3 heather, 0 no seasonal change.
varying float season_kind;
uniform sampler2D dabs : filter_linear_mipmap, repeat_enable;
// Colours picked from the card pine; the broadleaf sets its own.
uniform vec3 shade_col : source_color = vec3(0.11, 0.155, 0.145);
uniform vec3 base_col : source_color = vec3(0.16, 0.22, 0.2);
uniform vec3 lit_col : source_color = vec3(0.22, 0.29, 0.24);
uniform vec3 ink : source_color = vec3(0.1, 0.11, 0.08);
uniform vec3 bark_dark : source_color = vec3(0.24, 0.16, 0.19);
uniform vec3 bark_lit : source_color = vec3(0.39, 0.31, 0.27);
// Shade leans violet, as a painter's would; lit tips lean warm.
uniform vec3 shade_tint : source_color = vec3(0.72, 0.7, 0.9);
uniform vec3 warm_tint : source_color = vec3(1.0, 0.98, 0.92);
uniform float dab_size = 3.5;
uniform float cone_light = 0.75;
uniform float crown_y = -1.0;
uniform float tree_height = 4.4;
varying vec3 obj;
varying vec3 cone_n;
varying float dab;

// The brush noise at a world point, read on two planes so it sticks to the tree
// from every side; stretched a little sideways, like short strokes.
float dab_at(vec3 p) {
    vec2 a = vec2(p.x * 0.8, p.y * 1.3) / dab_size;
    vec2 b = vec2(p.z * 0.8, p.y * 1.3) / dab_size + 0.37;
    return mix(texture(dabs, a).r, texture(dabs, b).r, 0.5);
}

// The painted colour of a point: the bark in vertical strokes; the dark heart;
// a bough's deep green with one lighter patch along its upper middle, joined by
// dabs across neighbouring boughs, darker toward the trunk and sides, ink on the tips.
vec3 painted_color(vec2 uv, vec2 uv2, bool front, float up) {
    if (uv2.x < 0.5) {
        vec3 o = obj / tree_height;
        float ang = atan(o.x, o.z);
        float stroke = fract(ang * 1.4 + sin(o.y * 18.0 + ang * 3.0) * 0.12);
        return mix(bark_dark, bark_lit, 0.7 * smoothstep(0.55, 0.62, stroke) * (1.0 - smoothstep(0.78, 0.85, stroke)));
    }
    if (uv2.x < 1.5) return shade_col;
    float across = abs(uv.x), along = uv.y, rnd = uv2.y;
    float edge = 0.55 + 0.25 * (dab - 0.5) + 0.12 * rnd;
    float patch = (1.0 - smoothstep(edge - 0.04, edge + 0.04, across))
        * smoothstep(0.2 + 0.12 * rnd, 0.28 + 0.12 * rnd, along)
        * (1.0 - smoothstep(0.8, 0.86, along));
    float spots = smoothstep(0.6, 0.63, dab) * smoothstep(0.1, 0.3, along);
    patch = max(patch * 0.85, spots);
    patch *= front ? smoothstep(0.05, 0.3, up) : 0.0;
    vec3 col = mix(base_col, lit_col, patch);
    col *= mix(vec3(1.0), warm_tint, smoothstep(0.7, 0.9, along) * patch);
    col = mix(col, shade_col, 0.65 * smoothstep(0.72, 0.98, across) * (1.0 - patch));
    col = mix(shade_col, col, smoothstep(0.16, 0.4, along + (dab - 0.5) * 0.15));
    if (!front) col = mix(col, shade_col, 0.6);
    float line = smoothstep(0.9, 1.0, across) * smoothstep(0.7, 0.9, along) * 0.5 + smoothstep(0.9, 1.0, along);
    return mix(col, ink, 0.7 * clamp(line, 0.0, 1.0));
}

void vertex() {
    season_kind = INSTANCE_CUSTOM.w;
    // Broadleaf canopies thin to nothing in winter and grow back in spring.
    if (sway > 0.0 && season_kind > 0.5 && season_kind < 1.5) {
#ifdef PAINTED_FOLIAGE
    // Only the leaves go: they shrink into the crown's heart, the trunk stays.
    if (UV2.x > 0.5) { vec3 heart = vec3(0.0, max(crown_y, 0.5 * tree_height), 0.0); VERTEX = heart + (VERTEX - heart) * (1.0 - 0.85 * leaf_drop()); }
#else
    VERTEX *= 1.0 - 0.85 * leaf_drop();
#endif
}
    fade = fade_override >= 0.0 ? fade_override : see_through ? fade_amount(MODEL_MATRIX, CAMERA_POSITION_WORLD) : 0.0;
    // A tree or rock on a cell that was just cleared goes away instead of
    // vanishing: trees topple slowly and settle into the ground, rocks sink.
    // On a cell that comes back they grow in. Custom data: x when it started,
    // y +1 going or -1 coming, z 1 for rocks.
    if (INSTANCE_CUSTOM.x > 0.0) {
        float age = world_seconds - INSTANCE_CUSTOM.x;
        if (INSTANCE_CUSTOM.y < 0.0) {
            VERTEX *= smoothstep(0.0, 1.6, age);
        } else if (INSTANCE_CUSTOM.z > 0.5) {
            float k = smoothstep(0.0, 1.6, age);
            VERTEX.y -= k * 1.4;
            VERTEX *= 1.0 - k * k;
        } else {
            float k = clamp(age / 2.4, 0.0, 1.0);
            // Falls slow then fast, in a direction seeded by where the tree
            // stands, so every player sees it fall the same way (D-056).
            float dir = fract(sin(dot(floor(MODEL_MATRIX[3].xz + 0.5), vec2(12.9898, 78.233))) * 43758.5453) * 6.2832;
            vec3 axis = normalize(inverse(mat3(MODEL_MATRIX)) * vec3(sin(dir), 0.0, -cos(dir)));
            float angle = 1.45 * smoothstep(0.0, 0.7, k) * smoothstep(0.0, 0.7, k);
            float cs = cos(angle), sn = sin(angle);
            VERTEX = VERTEX * cs + cross(axis, VERTEX) * sn + axis * dot(axis, VERTEX) * (1.0 - cs);
            NORMAL = NORMAL * cs + cross(axis, NORMAL) * sn + axis * dot(axis, NORMAL) * (1.0 - cs);
            VERTEX *= 1.0 - smoothstep(0.6, 1.0, k);
        }
    }
    vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    if (sway > 0.0) {
        float h = clamp(VERTEX.y / sway_height, 0.0, 1.5);
        float t = TIME * 0.9 + world.x * 0.21 + world.z * 0.17;
        float gust = (0.45 + 0.45 * wind_gust + 0.15 * sin(TIME * 0.23 + world.x * 0.03)) * (0.5 + wind_strength * 1.6);
        VERTEX.x += sin(t) * sway * h * h * gust;
        VERTEX.z += cos(t * 0.73) * sway * 0.6 * h * h * gust;
    }
    world_pos = world;
    world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
    obj = VERTEX;
    // The crown's own normal: out from the trunk and up (a pine's cone), or out
    // from the heart of an egg-shaped crown (a broadleaf).
    vec3 out_dir = length(VERTEX.xz) > 1e-4 ? normalize(vec3(VERTEX.x, 0.0, VERTEX.z)) : vec3(0.0, 0.0, 1.0);
    vec3 crown = crown_y < 0.0 ? out_dir + vec3(0.0, 0.45, 0.0) : (VERTEX - vec3(0.0, crown_y, 0.0)) * vec3(1.0, 0.75, 1.0);
    cone_n = normalize((MODELVIEW_MATRIX * vec4(crown, 0.0)).xyz);
    // Palette colors are authored in sRGB.
    tint = pow(COLOR.rgb, vec3(2.2)) * albedo.rgb;
    // Ground that was just cleared or grew back blends from its old color.
    if (CUSTOM0.a > 0.0) {
        float k = smoothstep(0.0, 1.6, world_seconds - CUSTOM0.a);
        tint = mix(pow(CUSTOM0.rgb, vec3(2.2)) * albedo.rgb, tint, k);
    }
#ifdef GHOST
    // The ghost draws only trees in the way; the rest collapse to nothing.
    if (fade < 0.002) VERTEX = vec3(0.0);
#else
    // A tree in the way is drawn by its ghost instead.
    if (fade >= 0.002) VERTEX = vec3(0.0);
#endif
}

// World-space detail projected from three sides, so it wraps any shape
// without UVs.
float relief(vec3 p, vec3 n) {
    vec3 w = pow(abs(n), vec3(4.0));
    w /= (w.x + w.y + w.z);
    vec3 q = p * detail_stretch * detail_scale * 0.25;
    return texture(detail, q.zy).r * w.x + texture(detail, q.xz).r * w.y + texture(detail, q.xy).r * w.z;
}

void fragment() {
    vec2 uv = world_pos.xz * brush_scale + vec2(world_pos.y * 0.21);
    float a = texture(brush, uv).r;
    float b = texture(brush, uv * 2.7 + vec2(0.31, 0.77)).r;
    float paint = (a - 0.5) * brush_strength + (b - 0.5) * brush_strength * 0.6;

    // Relief darkens the crevices and bends the light around bumps.
    float h = relief(world_pos, world_normal);
    #ifdef PAINTED_FOLIAGE
    dab = dab_at(world_pos);
    vec3 painted = painted_color(UV, UV2, FRONT_FACING, world_normal.y);
    float paint_value = dot(painted, vec3(0.3, 0.59, 0.11)) / 0.25;
    vec3 color = painted * tint;
#else
    vec3 color = tint * (1.0 + paint * 2.0);
#endif
    color *= mix(1.0 - cavity, 1.0 + cavity * 0.35, smoothstep(0.15, 0.75, h));
    // Rain soaks upward-facing ground first; everything else darkens a little.
    wet = wetness * mix(0.45, 1.0, smoothstep(0.4, 0.9, world_normal.y)) * mix(0.75, 1.0, h);
    color *= mix(1.0, 0.8, wet);
    // Broadleaf leaves turn gold, orange and red through autumn, each tree
    // its own mix, then brown before they fall.
    if (sway > 0.0 && season_kind > 0.5 && season_kind < 1.5 && season_autumn > 0.0) {
        float seed = fract(sin(dot(floor(world_pos.xz * 0.25), vec2(12.9898, 78.233))) * 43758.5453);
        vec3 turned = mix(vec3(0.62, 0.42, 0.08), vec3(0.58, 0.16, 0.06), seed);
        turned = mix(turned, vec3(0.30, 0.20, 0.11), smoothstep(0.75, 1.0, season_autumn));
        #ifdef PAINTED_FOLIAGE
        if (UV2.x > 0.5) color = mix(color, turned * paint_value * 0.8, smoothstep(0.0, 0.6, season_autumn));
#else
        color = mix(color, turned * (1.0 + paint * 2.0), smoothstep(0.0, 0.6, season_autumn));
#endif
    }
    // Heather stays a quiet green-brown most of the year; only through late
    // summer do its flower tips turn a soft, dusty mauve, no brighter than
    // the grass (Justin: no saturated pink). The tips are found by their
    // colour (purple-ish vertex paint), so the mesh's own purple is tamed too.
    if (sway > 0.0 && season_kind > 2.5 && season_kind < 3.5) {
        float bloom = smoothstep(0.3, 0.38, season_year) * (1.0 - smoothstep(0.48, 0.56, season_year));
        float tip = smoothstep(0.02, 0.12, min(color.r, color.b) - color.g);
        float bright = dot(color, vec3(0.3, 0.59, 0.11));
        vec3 muted = mix(vec3(0.40, 0.38, 0.25), vec3(0.47, 0.41, 0.44), bloom * 0.8);
        color = mix(color, muted * clamp(bright / 0.5, 0.7, 1.1), tip);
    }
    // Snow settles on whatever faces up, filling the hollows first.
    float snow = snow_cover * smoothstep(0.3, 0.75, world_normal.y) * smoothstep(0.0, 0.5, snow_cover * 1.4 - h * 0.6);
    color = mix(color, SNOW * (1.0 + paint), snow);
    wet *= 1.0 - snow;
    // Puddles gather on flat ground in the low spots of a slow pattern,
    // spreading as the ground stays soaked and shrinking as it dries:
    // dark, still and glossy.
    float puddle = 0.0;
    if (puddles_ok > 0.5 && puddles > 0.01) {
        float low = 1.0 - texture(detail, world_pos.xz * 0.035).r;
        float edge = 1.0 - 0.42 * puddles;
        puddle = smoothstep(edge, edge + 0.05, low) * smoothstep(0.93, 0.99, world_normal.y) * (1.0 - snow);
        color *= mix(1.0, 0.5, puddle);
        wet = max(wet, puddle);
    }
    ALBEDO = clamp(color, 0.0, 1.0);
    shade = cloud_shadow(world_pos);

    vec3 dpdx = dFdx(VERTEX), dpdy = dFdy(VERTEX);
    vec3 r1 = cross(dpdy, NORMAL), r2 = cross(NORMAL, dpdx);
    float det = dot(dpdx, r1);
    vec3 grad = sign(det) * (dFdx(h) * r1 + dFdy(h) * r2);
    // Relief height is in tenths of a meter at most, so bumps stay subtle.
    NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);
#ifdef PAINTED_FOLIAGE
    // Light falls on the crown as a whole, in big painted shapes.
    if (UV2.x > 0.5) NORMAL = normalize(mix(FRONT_FACING ? NORMAL : -NORMAL, cone_n, cone_light));
#endif

    // The rim of the distant land melts into the fog, so its edge never
    // shows against the sky, even on a clear day.
    // Just out of the water, the player is darker and glossier up to where the water
    // reached, with a soft edge, drying off over time.
    if (wet_amount > 0.0) {
        float near_player = 1.0 - smoothstep(0.45, 0.65, length(world_pos.xz - player_position.xz));
        float soaked = wet_amount * near_player * (1.0 - smoothstep(wet_line - 0.04, wet_line + 0.04, world_pos.y));
        ALBEDO *= mix(1.0, 0.62, soaked);
        wet = max(wet, soaked);
    }

    if (far_land_radius > 0.0) {
        float rim = smoothstep(0.75 * far_land_radius, far_land_radius, length(world_pos.xz - player_position.xz));
        ALBEDO *= 1.0 - rim;
        EMISSION = fog_tint * rim;
    }

    AO = mix(1.0, smoothstep(0.0, 0.6, h), cavity * 0.6);
    AO_LIGHT_AFFECT = 0.3;
    ROUGHNESS = mix(mix(1.0, 0.75, wet), 0.05, puddle);
    SPECULAR = mix(0.08 * wet, 0.5, puddle);
    // Still water lies flat, whatever the relief of the ground under it.
    NORMAL = normalize(mix(NORMAL, (VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz, puddle));
#ifdef GHOST
    // Faintly see-through all over, and clear in a soft circle around the
    // player, measured on screen.
    vec4 target = VIEW_MATRIX * vec4(player_position + vec3(0.0, 0.9, 0.0), 1.0);
    float apart = length(VERTEX.xy / -VERTEX.z - target.xy / -target.z);
    float window = 1.0 - smoothstep(0.09, 0.3, apart);
    // Fully clear right around the player, so even several layers of
    // canopy never hide them.
    if (cut_mode > 1.5) {
        // cut_floor 1 clears the whole roof (player inside); 0 clears only the
        // circle round the player (player behind the building).
        ALPHA = 1.0 - fade * mix(cut_floor, 1.0, window);
    } else if (cut_mode > 0.5) {
        vec2 out_dir = world_pos.xz - cut_origin.xz;
        float facing = smoothstep(0.05, 0.45, dot(normalize(out_dir + vec2(1e-4)), cam_dir));
        ALPHA = 1.0 - fade * facing * mix(cut_floor, 1.0, window);
    } else {
        ALPHA = 1.0 - fade * (0.35 + 0.65 * window);
    }
    if (ALPHA < 0.04) discard;
#endif
}

// Wrapped diffuse light, so forms turn softly into shadow, plus a soft
// glint of sun on wet surfaces.
void light() {
#ifdef PAINTED_FOLIAGE
    float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
    float elevation = (INV_VIEW_MATRIX * vec4(LIGHT, 0.0)).y;
    float low_sun = LIGHT_IS_DIRECTIONAL ? 1.0 - smoothstep(0.2, 0.55, elevation) : 0.0;
    float wrap = mix(0.4, 0.85, low_sun);
    float wrapped = clamp(dot(NORMAL, LIGHT) * (1.0 - wrap) + wrap, 0.0, 1.0);
    float l = wrapped * ATTENUATION * sky + (dab - 0.5) * 0.22;
    float soft = 0.025;
    float mid = smoothstep(0.3 - soft, 0.3 + soft, l);
    float lit = smoothstep(0.62 - soft, 0.62 + soft, l);
    float band = 0.1 + 0.3 * low_sun + 0.36 * mid + 0.3 * lit;
    vec3 tone = mix(shade_tint, vec3(1.0), mid);
    float reach = LIGHT_IS_DIRECTIONAL ? 1.0 : smoothstep(0.0, 0.25, ATTENUATION);
    DIFFUSE_LIGHT += LIGHT_COLOR * tone * band * reach / PI;
    if (LIGHT_IS_DIRECTIONAL) {
        float back = max(0.0, dot(VIEW, -LIGHT));
        float edge = 1.0 - abs(dot(NORMAL, VIEW));
        DIFFUSE_LIGHT += LIGHT_COLOR * sky * (back * back * (0.35 + 0.5 * edge)) * mix(0.25, 1.0, low_sun) / PI;
    }
#else

    float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
    float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
    DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION * sky / PI;
    if (wet > 0.01) {
        vec3 half_dir = normalize(LIGHT + VIEW);
        float glint = pow(max(dot(NORMAL, half_dir), 0.0), 24.0) * wet * 0.06;
        SPECULAR_LIGHT += LIGHT_COLOR * glint * ATTENUATION * sky;
    }
#endif
}
