#ifndef __COLOR_SHADER_CGINC
#define __COLOR_SHADER_CGINC

#include "Gradient.cginc"

static float4 calculate_gradient_color(float2 uv, float4 color1, float4 color2, float angle, float2 stops, float2 center, float2 radius) {
    #if defined(COLOR_SOLID)
    return color1;
    #elif defined(COLOR_LINEAR_GRADIENT)
    return CalculateLinearGradient(uv, color1, color2, angle, stops);
    #elif defined(COLOR_RADIAL_GRADIENT)
    return CalculateRadialGradient(uv, color1, color2, center, radius, stops);
    #elif defined(COLOR_CONIC_GRADIENT)
    return CalculateConicGradient(uv, color1, color2, center, angle, stops);
    #else
    return color1;
    #endif
}

#endif
