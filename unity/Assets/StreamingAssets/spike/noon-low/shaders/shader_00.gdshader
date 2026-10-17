shader_type sky;

uniform vec3 top_color : source_color;
uniform vec3 horizon_color : source_color;
uniform vec3 ground_color : source_color;
uniform vec3 sun_color : source_color;
uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
uniform vec3 moon_dir = vec3(0.0, -1.0, 0.0);
uniform float daylight = 1.0;
uniform float cloud_cover = 0.2;
uniform vec2 cloud_offset;
uniform float stars = 0.0;
uniform sampler2D clouds : repeat_enable, filter_linear_mipmap;
// Weather on its way (AtmosphereFronts): where it comes from, how cloudy
// and wet it will be, and how strongly it shows yet; and a rainbow.
uniform vec3 front_dir = vec3(-0.6, 0.0, -0.8);
uniform float front_cloud = 0.2;
uniform float front_rain = 0.0;
uniform float front_amount = 0.0;
uniform float rainbow = 0.0;
// The prototype colour grade (AtmosphereGrade), 0 off.
uniform float grade = 0.0;
// Thunderstorms (AtmosphereStorms): how stormy, and lightning inside
// the clouds toward flash_dir.
uniform float storm = 0.0;
uniform float flash = 0.0;
uniform vec3 flash_dir = vec3(0.0, 0.25, -1.0);

float hash(vec3 p) {
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

void sky() {
    vec3 d = normalize(EYEDIR);
    float up = clamp(d.y, 0.0, 1.0);
    vec3 col = mix(horizon_color, top_color, pow(up, 0.45));
    col = mix(col, ground_color, 1.0 - smoothstep(-0.25, 0.0, d.y));

    // A soft glow around the sun, and its disc.
    float s = max(dot(d, sun_dir), 0.0);
    float sun_vis = smoothstep(-0.15, 0.05, sun_dir.y);
    col += sun_color * (pow(s, 5.0) * 0.3 + pow(s, 60.0) * 0.5) * sun_vis;
    col += sun_color * smoothstep(0.9993, 0.9997, s) * 6.0 * step(-0.02, sun_dir.y);

    float night = 1.0 - daylight;
    float m = max(dot(d, moon_dir), 0.0);
    col += vec3(0.85, 0.9, 1.0) * smoothstep(0.99955, 0.9998, m) * 2.0 * night;
    col += vec3(0.25, 0.3, 0.45) * pow(m, 24.0) * 0.3 * night;
    // A pale ring around the moon when thin cloud veils it.
    float veil = smoothstep(0.25, 0.5, cloud_cover) * (1.0 - smoothstep(0.75, 0.95, cloud_cover));
    col += vec3(0.75, 0.8, 0.9) * exp(-pow((m - 0.927) / 0.008, 2.0)) * veil * night * 0.08 * step(0.0, moon_dir.y);

    // How much of this direction belongs to the coming weather: the
    // upwind side, low in the sky.
    vec2 flat_dir = normalize(d.xz + vec2(0.0001));
    float upwind = smoothstep(0.1, 0.85, dot(flat_dir, normalize(front_dir.xz))) * (1.0 - smoothstep(0.05, 0.55, d.y)) * front_amount;

    if (d.y > 0.0) {
        if (stars > 0.0) {
            // A band of the galaxy arching across a clear night: faint,
            // mottled, and thick with extra stars.
            vec3 axis = normalize(vec3(0.35, 0.45, 0.82));
            float band = exp(-pow(dot(d, axis) / 0.2, 2.0));
            float dust = texture(clouds, d.xz / (d.y + 0.4) * 0.35 + vec2(0.13, 0.71)).r;
            float high = smoothstep(0.0, 0.3, d.y);
            col += vec3(0.50, 0.55, 0.72) * band * smoothstep(0.3, 0.8, dust) * 0.09 * stars * high;

            vec3 p = d * 260.0;
            float r = hash(floor(p));
            float threshold = 0.985 - 0.01 * band;
            float star = step(threshold, r) * smoothstep(0.35, 0.0, length(fract(p) - 0.5)) * (r - threshold) * 66.0;
            // Each star twinkles at its own pace and has its own tint,
            // from pale blue to warm white.
            float twinkle = 0.75 + 0.25 * sin(TIME * (1.5 + r * 3.0) + r * 90.0);
            vec3 tint = mix(vec3(0.80, 0.88, 1.0), vec3(1.0, 0.92, 0.80), fract(r * 37.0));
            col += tint * star * twinkle * stars * high * 1.4;
        }
        // The same cloud layer whose shadows drift over the ground.
        vec2 uv = (d.xz / (d.y + 0.03) * 120.0 + cloud_offset) * 0.01;
        float n = texture(clouds, uv).r;
        float cover = mix(cloud_cover, max(cloud_cover, front_cloud), upwind);
        float edge = 1.0 - cover;
        float c = smoothstep(edge - 0.08, edge + 0.22, n) * smoothstep(0.0, 0.18, d.y);
        float lit = 0.25 + 0.75 * daylight;
        vec3 cloud_col = mix(horizon_color * 1.1, vec3(0.92, 0.92, 0.95) * lit + sun_color * 0.15 * sun_vis, 0.5 + 0.5 * n);
        cloud_col *= 1.0 - 0.45 * cover * smoothstep(0.4, 0.8, n);
        // Rain clouds on the way are darker underneath.
        cloud_col *= 1.0 - 0.5 * front_rain * upwind;
        // Storm clouds hang heavy and dark, bruised blue-grey underneath.
        cloud_col = mix(cloud_col, cloud_col * vec3(0.55, 0.6, 0.68), storm * smoothstep(0.3, 0.7, n));
        // With the grade on, clouds near a low sun glow with warm rims.
        float sun_low = smoothstep(-0.05, 0.05, sun_dir.y) * (1.0 - smoothstep(0.2, 0.45, sun_dir.y));
        cloud_col += sun_color * pow(s, 4.0) * (1.0 - smoothstep(0.35, 0.8, n)) * 1.4 * sun_low * grade;
        col = mix(col, cloud_col, c * 0.9);

        // Lightning lights the clouds from inside, brightest toward the strike.
        float toward = pow(max(dot(d, flash_dir), 0.0), 5.0);
        col += vec3(0.72, 0.78, 1.0) * flash * toward * (0.35 + 1.4 * c * n);

        // Grey curtains of rain hanging from them down to the horizon.
        float around = atan(d.x, d.z);
        float streak = texture(clouds, vec2(around * 2.0 + cloud_offset.x * 0.002, d.y * 0.6 + TIME * 0.004)).r;
        float curtain = front_rain * upwind * (1.0 - smoothstep(0.0, 0.16, d.y)) * smoothstep(0.35, 0.75, streak);
        col = mix(col, horizon_color * mix(0.55, 0.8, daylight), curtain * 0.7);

        // A rainbow 42 degrees from the point opposite the sun, red outside.
        if (rainbow > 0.001) {
            float r = dot(d, -sun_dir);
            float t = (r - 0.743) / 0.022;
            float band = exp(-t * t);
            vec3 hue = clamp(vec3(1.0 - abs(t + 0.7) * 1.4, 1.0 - abs(t) * 1.4, 1.0 - abs(t - 0.7) * 1.4), 0.0, 1.0);
            col += hue * band * rainbow * 0.22 * smoothstep(0.0, 0.08, d.y) * (1.0 - c * 0.7);
        }
    }
    COLOR = col;
}