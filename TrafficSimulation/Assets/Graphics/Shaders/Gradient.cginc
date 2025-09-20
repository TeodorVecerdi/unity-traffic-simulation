#ifndef __GRADIENT_INC
#define __GRADIENT_INC

#include "oklab.cginc"

inline float __GradientIGN(float2 p) {
    float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
    return frac(magic.z * frac(dot(p, magic.xy)));
}

inline float IGNTPDF(float2 pix) {
    float a = __GradientIGN(pix);
    float b = __GradientIGN(pix + float2(19.0, 73.0));
    float c = __GradientIGN(pix + float2(113.0, 37.0));
    float d = __GradientIGN(pix + float2(57.0, 229.0));
    return ((a + b + c + d) * 0.5) - 1.0; // [-1,1]
}

inline float __GradientGenNoise(const float2 uv) {
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
}

inline float __Brightness(const float4 color) {
    return dot(color.rgb, float3(0.299, 0.587, 0.114));
}

inline float4 __GradientAddNoise(const float2 uv, const float4 color) {
    float brightness = 1.0;//sqrt(__Brightness(color));
    const float base_noise = (__GradientGenNoise(uv) - 0.5) * 0.01 * brightness;
    const float ign = 0.0;//(IGNTPDF(uv * 100.0)) * 0.01 * brightness;
    const float noise = base_noise + ign;
    return float4(color.rgb + noise, color.a);
}

inline float __GradientGetInterpolationFactor(const float x, const float2 stops) {
    return saturate((x - stops.x) / (stops.y - stops.x));
}

inline float4 __GradientInterpolate(const float2 uv, const float4 color1, const float4 color2, const float t) {
    const float3 color_oklab = lerp(LRGBtoOKLAB(color1.rgb), LRGBtoOKLAB(color2.rgb), t);
    const float alpha = lerp(color1.a, color2.a, t);
    const float4 finalColor = float4(OKLABtoLRGB(color_oklab), alpha);
    return __GradientAddNoise(uv, finalColor);
}

float4 CalculateLinearGradient(const float2 uv, const float4 color1, const float4 color2, const float angle, const float2 stops) {
    float sin, cos;
    sincos(angle, sin, cos);

    const float2 centered_uv = uv * 2.0 - 1.0;
    const float2 rotated_uv = float2(centered_uv.x * cos - centered_uv.y * sin, centered_uv.x * sin + centered_uv.y * cos);

    float t = __GradientGetInterpolationFactor(0.5 * (rotated_uv.y + 1.0), stops);
    return __GradientInterpolate(uv, color1, color2, t);
}

float4 CalculateRadialGradient(const float2 uv, const float4 color1, const float4 color2, const float2 center, const float2 radius, const float2 stops) {
    // Convert UV to centered coordinates
    const float2 centered_uv = uv - center;

    // Calculate distance from the center, normalized by radius
    // Using separate X and Y radius allows for elliptical gradients
    const float2 normalized_dist = centered_uv / radius;
    const float dist = length(normalized_dist);

    // Calculate interpolation factor
    float t = __GradientGetInterpolationFactor(dist, stops);
    return __GradientInterpolate(uv, color1, color2, t);
}

float4 CalculateConicGradient(const float2 uv, const float4 color1, const float4 color2, const float2 center, const float start_angle, const float2 stops) {
    // Convert UV to centered coordinates
    const float2 centered_uv = uv - center;

    // Calculate angle from center
    float angle = -atan2(centered_uv.y, centered_uv.x) + UNITY_PI * 0.5;
    // Adjust angle to match start angle
    angle = (angle - start_angle) / (2.0 * UNITY_PI);
    // Normalize angle to [0, 1] range
    angle = frac(angle);

    // Calculate interpolation factor
    float t = __GradientGetInterpolationFactor(angle, stops);
    return __GradientInterpolate(uv, color1, color2, t);
}

#endif
