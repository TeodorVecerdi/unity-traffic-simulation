Shader "TeodorVecerdi/Color" {
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
                float2 angle : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 texcoord1 : TEXCOORD1;
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

            v2f vert(appdata_t v) {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color;
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                float2 uv = TRANSFORM_TEX(v.texcoord1.xy, _MainTex);
                OUT.texcoord1 = float3(uv.x, uv.y, v.angle.x);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target {
                const fixed4 baseColor = tex2D(_MainTex, IN.texcoord) * IN.color;
                const float2 uv = IN.texcoord1.xy;
                const float angle = IN.texcoord1.z * UNITY_PI / 180.0;

                fixed4 color = baseColor * calculate_gradient_color(uv, _Color1, _Color2, angle, _Stops.xy, _Center.xy, _Radius.xy);

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
