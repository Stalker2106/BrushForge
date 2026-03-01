shader_type spatial;
render_mode unshaded;

uniform vec3 flash_color : source_color = vec3(1.0, 1.0, 0.0);
uniform float flash_speed : hint_range(0.1, 10.0) = 2.0;
uniform float flash_intensity : hint_range(0.0, 1.0) = 0.5;

// screen texture from previous pass
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    // Sample the previous pass (what was already rendered at this pixel)
    vec3 prev = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;

    // Flash factor
    float pulse = abs(sin(TIME * flash_speed));

    // Blend flash on top of previous color
    ALBEDO = mix(prev, flash_color, pulse * flash_intensity);
}
