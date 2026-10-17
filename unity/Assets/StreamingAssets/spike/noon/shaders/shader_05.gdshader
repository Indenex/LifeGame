shader_type spatial;
render_mode cull_disabled, specular_disabled;
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

// Tufts shrink to nothing between 0.65 x fade_far and fade_far metres
// from the player (0 = never), so they are gone before the chunks
// that carry none (GrassTufts).
uniform float fade_far = 0.0;
// 1 on the grass tufts only: the art-reference colour bands.
uniform float stylised = 0.0;
global uniform vec3 player_position;

varying vec3 tint;
varying float tip;
varying float gust;
varying vec3 world_pos;
varying float shade;

void vertex() {
    if (fade_far > 0.0) {
        float away = length(MODEL_MATRIX[3].xz - player_position.xz);
        VERTEX *= 1.0 - smoothstep(0.65 * fade_far, fade_far, away);
    }
    // Flowers (season code 2) bloom only in spring and summer; snow
    // presses all grass down.
    float flowering = INSTANCE_CUSTOM.w > 1.5 ? smoothstep(-0.25, 0.1, season_warmth) * (1.0 - smoothstep(0.1, 0.4, season_autumn)) : 1.0;
    VERTEX *= flowering;
    VERTEX.y *= 1.0 - 0.75 * snow_cover;
    // Grass on a cell just cleared lies down and sinks away; grass on a
    // cell uncleared rises back up.
    if (INSTANCE_CUSTOM.x > 0.0) {
        float k = smoothstep(0.0, 1.6, world_seconds - INSTANCE_CUSTOM.x);
        float height = INSTANCE_CUSTOM.y > 0.0 ? 1.0 - k : k;
        VERTEX.y *= height;
        VERTEX.xz *= mix(0.7, 1.0, height);
    }
    vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_pos = world;
    tip = UV.y;
    // Long, slow waves rolling across the field, plus a small flutter.
    float wave = sin(dot(world.xz, vec2(0.23, 0.31)) - TIME * 1.3);
    gust = smoothstep(0.2, 1.0, wave);
    float flutter = sin(TIME * 3.1 + world.x * 1.7 + world.z * 2.3) * 0.03;
    float wind = (0.45 + wind_strength * 1.8) * (0.7 + 0.6 * wind_gust);
    vec3 bend = vec3(wind_dir.x, 0.0, wind_dir.y) * (0.06 + gust * 0.16) * wind + vec3(flutter, 0.0, -flutter) * wind;
    vec3 local = inverse(mat3(MODEL_MATRIX)) * bend;
    VERTEX.xz += local.xz * tip * tip;
    VERTEX.y -= gust * 0.06 * tip * tip;
    NORMAL = vec3(0.0, 1.0, 0.0);
    tint = pow(COLOR.rgb, vec3(2.2));
}

void fragment() {
    // Roots match the ground; tips catch the sun and turn warm.
    vec3 warm = tint * vec3(1.3, 1.3, 1.0) + vec3(0.02, 0.02, 0.0);
    vec3 color = mix(tint, warm, smoothstep(0.0, 1.0, tip));
    // The grass tufts lean toward the art references (docs/art-reference 02):
    // muted olive and warm yellow-green, a soft blue-violet shade at the
    // roots, and pale lit tips, in soft tone bands. Shape, height, density
    // and wind are unchanged; ferns, reeds and flowers keep their colours.
    if (stylised > 0.5) {
        float lum = dot(tint, vec3(0.3, 0.59, 0.11));
        vec3 olive = mix(tint, vec3(lum * 1.1, lum * 1.05, lum * 0.55), 0.3);
        vec3 root_shade = olive * vec3(0.62, 0.65, 0.82);
        vec3 lit_tip = olive * vec3(1.36, 1.3, 0.78);
        color = mix(root_shade, olive, smoothstep(0.05, 0.4, tip));
        color = mix(color, lit_tip, smoothstep(0.6, 0.95, tip));
    }
    color += vec3(0.05, 0.05, 0.02) * gust * tip;
    color *= mix(1.0, 0.88, wetness);
    // Grass fades to straw in the cold and wears snow on its tips.
    vec3 straw = vec3(0.42, 0.36, 0.20) * (0.7 + 0.5 * tip);
    color = mix(color, straw, 0.55 * max(0.0, -season_warmth));
    color = mix(color, SNOW, snow_cover * (0.55 + 0.45 * tip));
    ALBEDO = clamp(color, 0.0, 1.0);
    shade = cloud_shadow(world_pos);
    // Both faces of a blade are lit like the ground beneath it.
    NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
    ROUGHNESS = 1.0;
    SPECULAR = 0.0;
}

void light() {
    float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
    float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
    DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION * sky / PI;
}