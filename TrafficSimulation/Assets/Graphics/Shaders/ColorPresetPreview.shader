Shader "TeodorVecerdi/ColorPresetPreview" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1 ("Color 1", Color) = (1, 1, 1, 1)
        _Color2 ("Color 2", Color) = (1, 1, 1, 1)
        _Angle ("Angle", Float) = 0
        _Stops ("Stops", Vector) = (0, 1, 0, 0)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Vector) = (0.5, 0.5, 0, 0)
        [KeywordEnum(SolidColor, LinearGradient, RadialGradient, ConicGradient)] _Mode ("Mode", Float) = 0
    }

    SubShader {
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Graphics/Shaders/Gradient.cginc"

            #define MODE_SOLID_COLOR 0
            #define MODE_LINEAR_GRADIENT 1
            #define MODE_RADIAL_GRADIENT 2
            #define MODE_CONIC_GRADIENT 3

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color1;
            fixed4 _Color2;
            float _Angle;
            float4 _Stops;
            float4 _Center;
            float4 _Radius;
            float _Mode;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Signed distance field functions
            float sdBox(float2 p, float2 b, float r) {
                float2 d = abs(p) - b + r;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
            }

            fixed4 CalculateColor(float2 uv) {
                if (_Mode == MODE_SOLID_COLOR)
                    return _Color1;
                if (_Mode == MODE_LINEAR_GRADIENT)
                    return CalculateLinearGradient(uv, _Color1, _Color2, _Angle * UNITY_PI / 180.0, _Stops.xy);
                if (_Mode == MODE_RADIAL_GRADIENT)
                    return CalculateRadialGradient(uv, _Color1, _Color2, _Center.xy, _Radius.xy, _Stops.xy);
                if (_Mode == MODE_CONIC_GRADIENT)
                    return CalculateConicGradient(uv, _Color1, _Color2, _Center.xy, _Angle * UNITY_PI / 180.0, _Stops.xy);
                return fixed4(1, 0, 1, 1);
            }

            fixed4 frag(v2f i) : SV_Target {
                // Draw rounded rectangle background
                float box = sdBox(2.2 * (2.0 * i.uv - 1.0), float2(1, 1), -0.5);
                float mask = 1 - box;
                mask = smoothstep(0, 0.05, mask);

                // Calculate base color
                fixed4 baseColor = tex2D(_MainTex, i.uv);
                fixed4 color = baseColor * CalculateColor(i.uv);
                color.a *= mask;

                return color;
            }
            ENDCG
        }
    }
}
