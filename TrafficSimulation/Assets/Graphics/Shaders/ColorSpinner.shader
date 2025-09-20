Shader "TeodorVecerdi/ColorSpinner" {
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
            #pragma multi_compile_local _ COLOR_SOLID COLOR_LINEAR_GRADIENT COLOR_RADIAL_GRADIENT COLOR_CONIC_GRADIENT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/Graphics/Shaders/ColorShader.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 extraData0 : TEXCOORD2; // gradient angle, spinner radius, thickness, smoothness
                float3 extraData1 : TEXCOORD3; // arc length, spinner rotation, offset rotation by arc length
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                float4 extraData0 : TEXCOORD3;
                float3 extraData1 : TEXCOORD4;
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

            v2f vert(appdata_t v) {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color;
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                float2 uv = TRANSFORM_TEX(v.texcoord1.xy, _MainTex);
                OUT.texcoord1 = float2(uv.x, uv.y);
                OUT.extraData0 = v.extraData0;
                OUT.extraData1 = v.extraData1;
                return OUT;
            }

            float2 rotate(float2 uv, const float2 center, const float rotation) {
                uv -= center;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rMatrix = float2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                uv.xy = mul(uv.xy, rMatrix);
                uv += center;
                return uv;
            }

            // Source for sdArc function:
            // https://www.shadertoy.com/view/wl23RK
            // https://iquilezles.org/articles/distfunctions2d/
            float sd_arc(float2 p, const float2 sin_cos, const float radius, const float thickness) {
                p.x = abs(p.x);
                return (sin_cos.y * p.x > sin_cos.x * p.y ? length(p - sin_cos * radius) : abs(length(p) - radius)) - thickness;
            }

            float spinner(float2 base_uv, float radius, float thickness, float smoothness, float arc_length, float rotation, float offset_rotation_by_arc_length) {
                rotation += offset_rotation_by_arc_length * arc_length;
                const float2 sin_cos = float2(sin(arc_length), cos(arc_length));
                const float2 uv = rotate(2.0 * base_uv - 1.0, 0.0, -rotation);
                const float d = sd_arc(uv, sin_cos, radius, thickness);
                return (1.0 - smoothstep(0.0, smoothness, d));
            }

            fixed4 frag(v2f IN) : SV_Target {
                const float spinnerRadius = IN.extraData0.y;
                const float spinnerThickness = IN.extraData0.z;
                const float spinnerSmoothness = IN.extraData0.w;
                const float spinnerArcLength = clamp(IN.extraData1.x, 0.0, UNITY_PI);
                const float spinnerRotation = IN.extraData1.y;
                const float offsetRotationByArcLength = IN.extraData1.z;

                const fixed4 baseColor = tex2D(_MainTex, IN.texcoord) * IN.color;
                const float angle = IN.extraData0.x * UNITY_PI / 180.0;
                fixed4 color = baseColor * calculate_gradient_color(IN.texcoord1, _Color1, _Color2, angle, _Stops.xy, _Center.xy, _Radius.xy);

                color.a *= spinner(IN.texcoord, spinnerRadius, spinnerThickness, spinnerSmoothness, spinnerArcLength, spinnerRotation, offsetRotationByArcLength);

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
