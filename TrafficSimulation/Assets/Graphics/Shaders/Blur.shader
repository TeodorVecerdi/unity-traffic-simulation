// MIT License
//
// Copyright (c) 2024 Teodor Vecerdi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

Shader "TeodorVecerdi/Blur" {
    Properties {
        _MainTex ("Main Tex", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        [Toggle] _EnableTint ("Enable Tint", Float) = 0
        _TintAmount ("Tint Amount", Range(0.0, 1.0)) = 0.64
        _Tint ("Tint", Color) = (0.0, 0.0, 0.0, 0.0)
        [Toggle] _EnableVibrancy ("Enable Vibrancy", Float) = 0
        _Vibrancy ("Vibrancy", Range(-1.0, 2.0)) = 0.0
        [Toggle] _EnableNoise ("Enable Noise", Float) = 0
        _NoiseTex ("Noise", 2D) = "white" {}
        _ScreenResolution ("Screen Resolution", Vector) = (3840.0, 2160.0, 0.0, 0.0)
        _NoiseTextureResolution ("Noise Texture Resolution", Vector) = (64.0, 64.0, 0.0, 0.0)
    }

    SubShader {
        Pass {
            // Pass 0 - Horizontal
            Name "Horizontal"
            ZTest Always Cull Off ZWrite Off
            Fog {
                Mode off
            }
            Blend Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform half4 _MainTex_TexelSize;
            uniform half _BlurSize;

            half4 frag(const v2f_img i) : COLOR {
                half4 color = 0.16 * tex2D(_MainTex, i.uv);
                color += 0.15 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(1.0 * _BlurSize, 0.0));
                color += 0.15 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(1.0 * _BlurSize, 0.0));
                color += 0.12 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(2.0 * _BlurSize, 0.0));
                color += 0.12 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(2.0 * _BlurSize, 0.0));
                color += 0.09 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(3.0 * _BlurSize, 0.0));
                color += 0.09 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(3.0 * _BlurSize, 0.0));
                color += 0.06 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(4.0 * _BlurSize, 0.0));
                color += 0.06 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(4.0 * _BlurSize, 0.0));
                return color;
            }
            ENDCG
        }

        Pass {
            // Pass 1 - Vertical
            Name "Vertical"
            ZTest Always Cull Off ZWrite Off
            Fog {
                Mode off
            }
            Blend Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform half4 _MainTex_TexelSize;
            uniform half _BlurSize;

            half4 frag(const v2f_img i) : COLOR {
                half4 color = 0.16 * tex2D(_MainTex, i.uv);
                color += 0.15 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(0.0, 1.0 * _BlurSize));
                color += 0.15 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(0.0, 1.0 * _BlurSize));
                color += 0.12 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(0.0, 2.0 * _BlurSize));
                color += 0.12 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(0.0, 2.0 * _BlurSize));
                color += 0.09 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(0.0, 3.0 * _BlurSize));
                color += 0.09 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(0.0, 3.0 * _BlurSize));
                color += 0.06 * tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(0.0, 4.0 * _BlurSize));
                color += 0.06 * tex2D(_MainTex, i.uv - _MainTex_TexelSize.xy * half2(0.0, 4.0 * _BlurSize));
                return color;
            }
            ENDCG
        }

        Pass {
            // Pass 2 - Tint
            Name "Tint"
            ZTest Always Cull Off ZWrite Off
            Fog {
                Mode off
            }
            Blend Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform half4 _MainTex_TexelSize;

            uniform half _EnableTint;
            uniform half _TintAmount;
            uniform half4 _Tint;

            uniform half _EnableVibrancy;
            uniform half _Vibrancy;

            uniform half _EnableNoise;
            uniform sampler2D _NoiseTex;

            uniform float4 _ScreenResolution;
            uniform float4 _NoiseTextureResolution;

            static float random(const float2 coords) {
                return frac(sin(dot(coords.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            static half4 noise(const float2 uv, const float granularity) {
                return half4(
                    lerp(-granularity, granularity, random(uv)),
                    lerp(-granularity, granularity, random(uv + 0.333)),
                    lerp(-granularity, granularity, random(uv + 0.666)),
                    0.0
                );
            }

            static float3 sample_noise(float2 uv, float scale, float rotation) {
                const float2 scaled_uv = scale * uv; // * (1.0 / _MainTex_TexelSize.xy) * (1.0 / _ScreenResolution.xy) * (32.0 / _NoiseTextureResolution.xy);
                const float2 rotated_uv = float2(
                    cos(rotation) * scaled_uv.x - sin(rotation) * scaled_uv.y,
                    sin(rotation) * scaled_uv.x + cos(rotation) * scaled_uv.y
                );
                const half4 noise = tex2D(_NoiseTex, rotated_uv);
                return noise.rrr * 2.0 - 1.0;
            }

            half4 frag(v2f_img i) : COLOR {
                half4 color = tex2D(_MainTex, i.uv);

                float1 grey = dot(color.rgb, half3(0.299, 0.587, 0.114));
                color.rgb = lerp(color.rgb, lerp(grey.rrr, color.rgb, _Vibrancy + 1.0), _EnableVibrancy);
                color = lerp(color, _Tint, _EnableTint * _TintAmount);
                color.rgb += _EnableNoise * (0.0025 * sample_noise(i.uv, 70.0, 0.0) + 0.0025 * sample_noise(i.uv, 90.0, 1.2) + 0.0025 * sample_noise(i.uv, 120.0, 2.4));
                // color.rgb += _EnableNoise * 0.3 * noise(i.uv, 0.005);
                // color.rgb += _EnableNoise * 0.002 * random(i.uv);

                return color;
            }
            ENDCG
        }
    }

    Fallback off
}