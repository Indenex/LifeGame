shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, specular_schlick_ggx;
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

uniform vec4 water_color : source_color;
uniform vec4 shallow_color : source_color = vec4(0.30, 0.55, 0.55, 1.0);
uniform vec4 sky_tint : source_color = vec4(0.62, 0.72, 0.78, 1.0);
uniform sampler2D depth_texture : hint_depth_texture, filter_linear;
global uniform vec3 player_position;
varying vec3 world_pos;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

// Height of the slow wind ripples: a few soft waves crossing at angles.
float ripples(vec2 p, float t, float stir) {
    float h = sin(dot(p, vec2(0.9, 0.5)) * 1.7 + t * 1.1) * 0.5
            + sin(dot(p, vec2(-0.4, 1.0)) * 2.6 + t * 1.6) * 0.3
            + sin(dot(p, vec2(0.7, -0.8)) * 4.3 + t * 2.3) * 0.15;
    return h * stir;
}

// Rain rings: each cell of a grid gets drops at its own moments, and each drop
// sends out one ring that widens and fades.
vec3 rings(vec2 p, float t, float density) {
    vec2 slope = vec2(0.0);
    float crest = 0.0;
    for (int layer = 0; layer < 2; layer++) {
        vec2 q = p * 1.3 + float(layer) * 0.37;
        vec2 cell = floor(q);
        for (int dx = -1; dx <= 1; dx++)
        for (int dz = -1; dz <= 1; dz++) {
            vec2 c = cell + vec2(float(dx), float(dz));
            float h = hash(c + float(layer) * 17.0);
            if (h > density) continue;
            float rate = 0.7 + h * 0.6;
            float age = fract(t * rate + h * 7.0);
            // Each new drop lands somewhere else in its cell.
            float drop = floor(t * rate + h * 7.0);
            vec2 center = c + 0.1 + vec2(hash(c + drop * 1.37 + 3.1), hash(c + drop * 2.11 + 5.7)) * 0.8;
            vec2 d = q - center;
            float r = length(d);
            float radius = age * 0.55;
            float band = r - radius;
            float wave = sin(band * 40.0) * exp(-band * band * 260.0) * (1.0 - age) * (1.0 - age);
            slope += (r > 0.001 ? d / r : vec2(0.0)) * wave;
            crest += max(wave, 0.0);
        }
    }
    return vec3(slope, crest);
}

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec2 p = world_pos.xz;
    float t = TIME;
    float stir = 0.03 + 0.06 * wind_strength * (0.5 + 0.8 * wind_gust);
    float e = 0.05;
    float h0 = ripples(p, t, stir);
    vec2 slope = vec2(ripples(p + vec2(e, 0.0), t, stir) - h0, ripples(p + vec2(0.0, e), t, stir) - h0) / e;
    float rain = smoothstep(0.02, 0.6, rain_amount);
    float crest = 0.0;
    if (rain > 0.0) {
        vec3 ring = rings(p, t, mix(0.15, 0.9, rain));
        slope += ring.xy * 0.12 * rain;
        crest = min(ring.z, 1.0) * rain;
    }
    vec3 n = normalize(vec3(-slope.x, 1.0, -slope.y));
    NORMAL = normalize((VIEW_MATRIX * vec4(n, 0.0)).xyz);

    // Deeper water reads darker; the bank shows through the shallows.
    float depth = textureLod(depth_texture, SCREEN_UV, 0.0).r;
    vec4 behind = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth, 1.0);
    behind.xyz /= behind.w;
    float through = clamp(VERTEX.z - behind.z, 0.0, 10.0);
    vec3 color = pow(water_color.rgb, vec3(2.2));
    // Looking across the water it takes on the pale sky; looking down into it,
    // its own deep color. Soft, never a mirror.
    float across = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
    vec3 sky = pow(sky_tint.rgb, vec3(2.2));
    vec3 shallow = pow(shallow_color.rgb, vec3(2.2));
    // Ring crests and the steeper ripples catch a little of the sky.
    float glint = clamp(crest * 0.45 + length(slope) * 0.6, 0.0, 0.5);
    ALBEDO = mix(mix(shallow, color, smoothstep(0.0, 1.2, through)), sky, across * 0.35 + glint);
    // Even ankle-deep water reads as water; only a thin rim at the bank softens.
    ALPHA = water_color.a * mix(0.85, 1.0, smoothstep(0.05, 0.8, through)) * smoothstep(0.0, 0.04, through);
    ROUGHNESS = mix(0.32, 0.45, rain);
    SPECULAR = 0.12;
    METALLIC = 0.0;

    // Like the land, the water melts into the fog over the outer quarter of
    // the distant land, and past its edge there is none to see.
    if (far_land_radius > 0.0) {
        float reach = length(world_pos.xz - player_position.xz);
        if (reach > far_land_radius) discard;
        float rim = smoothstep(0.75 * far_land_radius, far_land_radius, reach);
        ALBEDO *= 1.0 - rim;
        EMISSION = fog_tint * rim;
        ALPHA = mix(ALPHA, 1.0, rim);
        SPECULAR *= 1.0 - rim;
        ROUGHNESS = mix(ROUGHNESS, 1.0, rim);
    }
}