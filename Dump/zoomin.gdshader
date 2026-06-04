shader_type canvas_item;

// ========================
// 输入纹理
// ========================
uniform sampler2D from_tex : filter_nearest;
uniform sampler2D to_tex;

// ========================
// 过渡控制
// ========================
uniform float progress : hint_range(0.0, 1.0) = 0.0;

// ========================
// Zoom Blur 参数
// ========================
uniform float blur_strength : hint_range(0.0, 1.0) = 0.3;
uniform int samples : hint_range(4, 32) = 20;
uniform vec2 blur_center = vec2(0.5, 0.5);

// ========================
// 缓动函数
// ========================
float exponential_ease_in_out(float t) {
	if (t <= 0.0 || t >= 1.0) return t;
	t *= 2.0;
	if (t < 1.0)
		return 0.5 * pow(2.0, 10.0 * (t - 1.0));
	return 0.5 * (-pow(2.0, -10.0 * (t - 1.0)) + 2.0);
}

// ========================
// Cross fade
// ========================
vec4 cross_fade(vec2 uv, float dissolve) {
	vec4 from_col = texture(from_tex, uv);
	vec4 to_col   = texture(to_tex, uv);
	return mix(from_col, to_col, dissolve);
}

// ========================
// Fragment
// ========================
void fragment() {
	vec2 uv = UV;

	// 最终的清晰采样（永远正确）
	vec4 clean_to = texture(to_tex, uv);

	// dissolve 用于 from → to
	float dissolve = exponential_ease_in_out(progress);

	// blur 只在前 85% 生效
	float blur_mix = 1.0 - smoothstep(0.85, 1.0, progress);

	// blur 强度
	float strength = sin(progress * PI) * blur_strength * blur_mix;

	vec2 to_center = blur_center - uv;

	vec4 blurred = vec4(0.0);
	float total = 0.0;

	for (int i = 0; i < samples; i++) {
		float percent = float(i) / float(samples - 1);
		float weight = percent * (1.0 - percent);

		vec2 sample_uv = uv + to_center * percent * strength;
		blurred += cross_fade(sample_uv, dissolve) * weight;
		total += weight;
	}

	blurred /= max(total, 0.0001);

	// 顺滑回到清晰 to_tex
	COLOR = mix(blurred, clean_to, 1.0 - blur_mix);
}
