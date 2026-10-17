shader_type spatial;
render_mode cull_disabled;
uniform vec3 shade_tint : source_color = vec3(0.6, 0.56, 0.84);
uniform float rim_dark = 0.58;
varying vec3 tint;
varying vec3 world_n;
void vertex() {
    tint = pow(COLOR.rgb, vec3(2.2));
    world_n = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}
void fragment() {
    vec3 c = tint * (0.94 + 0.12 * smoothstep(0.25, 0.75, world_n.y));
    float rim = 1.0 - clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0);
    c *= mix(1.0, rim_dark, smoothstep(0.62, 0.86, rim));
    ALBEDO = c;
    ROUGHNESS = 0.9;
    SPECULAR = 0.0;
}
void light() {
    float l = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
    // Shade, mid and lit, with crisp edges; the shade band leans violet.
    float lit = 0.35 * smoothstep(0.06, 0.1, l) + 0.65 * smoothstep(0.42, 0.47, l);
    vec3 tone = mix(shade_tint, vec3(1.0), smoothstep(0.06, 0.1, l));
    DIFFUSE_LIGHT += LIGHT_COLOR * tone * (0.2 + 0.6 * lit) / PI;
}