Shader "TeodorVecerdi/ColorMorph" {
    Properties {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        _Color1 ("Color 1", Color) = (1, 1, 1, 1)
        _Color2 ("Color 2", Color) = (1, 1, 1, 1)
        [ShowAsVector2] _Stops ("Stops", Vector) = (0, 1, 0, 0)
        [ShowAsVector2] _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        [ShowAsVector2] _Radius ("Radius", Vector) = (0.5, 0.5, 0, 0)
        _Angle ("Angle", Range(0, 360)) = 0

        _MorphProgress ("Morph Progress", Range(0, 1)) = 0
        [KeywordEnum(LINEAR, RADIAL, CONIC)] _GradientFrom ("From Gradient", Float) = 0
        [KeywordEnum(LINEAR, RADIAL, CONIC)] _GradientTo ("To Gradient", Float) = 1

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma multi_compile _GRADIENTFROM_LINEAR _GRADIENTFROM_RADIAL _GRADIENTFROM_CONIC
            #pragma multi_compile _GRADIENTTO_LINEAR _GRADIENTTO_RADIAL _GRADIENTTO_CONIC

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/Graphics/Shaders/Gradient.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _Color1;
            fixed4 _Color2;
            float4 _Stops;
            float4 _Center;
            float4 _Radius;
            float _MorphProgress;
            float _Angle;

            v2f vert(appdata_t v) {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color;
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.texcoord1 = TRANSFORM_TEX(v.texcoord1.xy, _MainTex);
                return OUT;
            }

            // Helper functions for morphing
            float GetLinearT(float2 uv, float angle) {
                float sin_a, cos_a;
                sincos(angle, sin_a, cos_a);
                float2 centered_uv = uv * 2.0 - 1.0;
                float2 rotated_uv = float2(
                    centered_uv.x * cos_a - centered_uv.y * sin_a,
                    centered_uv.x * sin_a + centered_uv.y * cos_a
                );
                return 0.5 * (rotated_uv.y + 1.0);
            }

            float GetRadialT(float2 uv) {
                float2 centered_uv = uv - _Center.xy;
                float2 normalized_dist = centered_uv / _Radius.xy;
                return length(normalized_dist);
            }

            float GetConicT(float2 uv, float angle) {
                float2 centered_uv = uv - _Center.xy;
                float current_angle = -atan2(centered_uv.y, centered_uv.x) + UNITY_PI * 0.5;
                return frac((current_angle - angle) / (2.0 * UNITY_PI));
            }

            float MorphLinearRadial(float2 uv, float angle, float t) {
                float linear_t = GetLinearT(uv, angle);
                float radial_t = GetRadialT(uv);
                return lerp(linear_t, radial_t, t);
            }

            float MorphRadialConic(float2 uv, float angle, float t) {
                float radial_t = GetRadialT(uv);
                float conic_t = GetConicT(uv, angle);
                return lerp(radial_t, conic_t, t);
            }

            float MorphLinearConic(float2 uv, float angle, float t) {
                float linear_t = GetLinearT(uv, angle);
                float conic_t = GetConicT(uv, angle);
                return lerp(linear_t, conic_t, t);
            }

            float4 CalculateMorphedColor(float2 uv) {
                float angle = _Angle * UNITY_PI / 180.0;
                float t;

                #if defined(_GRADIENTFROM_LINEAR)

                #if defined(_GRADIENTTO_LINEAR)
                t = GetLinearT(uv, angle);
                #elif defined(_GRADIENTTO_RADIAL)
                t = MorphLinearRadial(uv, angle, _MorphProgress);
                #elif defined(_GRADIENTTO_CONIC)
                t = MorphLinearConic(uv, angle, _MorphProgress);
                #endif

                #elif defined(_GRADIENTFROM_RADIAL)

                #if defined(_GRADIENTTO_RADIAL)
                t = GetRadialT(uv);
                #elif defined(_GRADIENTTO_CONIC)
                t = MorphRadialConic(uv, angle, _MorphProgress);
                #elif defined(_GRADIENTTO_LINEAR)
                t = MorphLinearRadial(uv, angle, 1.0 - _MorphProgress);
                #endif

                #elif defined(_GRADIENTFROM_CONIC)

                #if defined(_GRADIENTTO_CONIC)
                t = GetConicT(uv, angle);
                #elif defined(_GRADIENTTO_RADIAL)
                t = MorphRadialConic(uv, angle, 1.0 - _MorphProgress);
                #elif defined(_GRADIENTTO_LINEAR)
                t = MorphLinearConic(uv, angle, 1.0 - _MorphProgress);
                #endif

                #else
                t = GetLinearT(uv, angle);
                #endif

                t = __GradientGetInterpolationFactor(t, _Stops.xy);
                return __GradientInterpolate(uv, _Color1, _Color2, t);
            }

            fixed4 frag(v2f IN) : SV_Target {
                const fixed4 baseColor = tex2D(_MainTex, IN.texcoord) * IN.color;
                fixed4 color = baseColor * CalculateMorphedColor(IN.texcoord1);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
