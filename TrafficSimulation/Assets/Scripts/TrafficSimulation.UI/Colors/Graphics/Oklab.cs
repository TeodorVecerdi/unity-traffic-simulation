/*
 * MIT License
 *
 * Copyright (c) 2023 yum_food
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal
 * the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE
 * SOFTWARE.
 */

using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace TrafficSimulation.UI.Colors.Graphics;

internal static class Oklab {
    // Weights: https://en.wikipedia.org/wiki/SRGB
    public static float3 LrgbToXyz(float3 color) {
        var rgbToXyz = float3x3(
            0.4124f, 0.3576f, 0.1805f,
            0.2126f, 0.7152f, 0.0722f,
            0.0193f, 0.1192f, 0.9505f
        );

        return mul(rgbToXyz, color);
    }

    // Weights: https://en.wikipedia.org/wiki/SRGB
    public static float3 XyzToLrgb(float3 color) {
        var xyzToRgb = float3x3(
            3.24062548f, -1.53720797f, -0.4986286f,
            -0.96893071f, 1.87575606f, 0.04151752f,
            0.05571012f, -0.20402105f, 1.05699594f
        );

        return mul(xyzToRgb, color);
    }

    // Source: https://bottosson.github.io/posts/oklab/
    public static float3 XyzToOklab(float3 color) {
        var m1 = float3x3(
            0.8189f, 0.3618f, -0.1288f,
            0.0329f, 0.9293f, 0.0361f,
            0.0482f, 0.2643f, 0.6338f
        );
        var m2 = float3x3(
            0.2104f, 0.7936f, -0.0040f,
            1.9779f, -2.4285f, 0.4505f,
            0.0259f, 0.7827f, -0.8086f
        );

        color = mul(m1, color);
        color = pow(color, 0.33333333333f);
        return mul(m2, color);
    }

    // Source: https://bottosson.github.io/posts/oklab/
    public static float3 OklabToXyz(float3 color) {
        var im1 = float3x3(
            1.22700842f, -0.5576564f, 0.28111404f,
            -0.04047048f, 1.11219073f, -0.07157255f,
            -0.07643651f, -0.42138367f, 1.58625265f
        );
        var im2 = float3x3(
            1.00003964f, 0.39638005f, 0.21589049f,
            0.99998945f, -0.10553958f, -0.06374665f,
            0.99999105f, -0.08946276f, -1.291495f
        );

        color = mul(im2, color);
        color = pow(color, 3);
        return mul(im1, color);
    }

    // Source: https://bottosson.github.io/posts/oklab/
    public static float3 OklabToOklch(float3 color) {
        var c = length(color.yz);
        var h = atan2(color.z, color.y);
        return float3(color.x, c, h);
    }

    // Source: https://bottosson.github.io/posts/oklab/
    // Note: hue must be in units of radians.
    public static float3 OklchToOklab(float3 color) {
        var a = color.y * cos(color.z);
        var b = color.y * sin(color.z);
        return float3(color.x, a, b);
    }

    public static float3 LrgbToOklab(float3 color) {
        return XyzToOklab(LrgbToXyz(color));
    }

    public static float3 OklabToLrgb(float3 color) {
        return XyzToLrgb(OklabToXyz(color));
    }

    public static float3 LrgbToOklch(float3 color) {
        return OklabToOklch(XyzToOklab(LrgbToXyz(color)));
    }

    public static float3 OklchToLrgb(float3 color) {
        return XyzToLrgb(OklabToXyz(OklchToOklab(color)));
    }
}
