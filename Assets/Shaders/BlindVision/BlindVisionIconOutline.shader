// Kör görüşte (Şef) hotbar ikonu: ikonun RENGİ hiç okunmaz; yalnızca alfa kanalındaki siluetin kenarı beyaz çizgi
// olarak çizilir (GDD 4.1.1: Şef nesneleri kontur olarak görür). İçi boştur — yüzey rengi/deseni bilgi sızdırmaz.
Shader "CookNoEvil/UI/BlindVisionIconOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Kontur rengi", Color) = (1, 1, 1, 1)
        _Thickness ("Kalınlık (texel)", Range(0.5, 8)) = 3
        _AlphaThreshold ("Siluet eşiği", Range(0.01, 0.99)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _Thickness;
            float _AlphaThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // Dokunun dışı saydam sayılır (ikon kenara dayanıyorsa kontur orada da kapansın).
            float Inside(float2 uv)
            {
                float inBounds = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return step(_AlphaThreshold, tex2D(_MainTex, uv).a) * inBounds;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _Thickness;
                float lo = 1.0;
                float hi = 0.0;
                // Sekiz yönde komşular: içeride ve dışarıda komşusu olan piksel siluetin kenarındadır.
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * 0.7853982;
                    float a = Inside(i.uv + float2(cos(angle), sin(angle)) * d);
                    lo = min(lo, a);
                    hi = max(hi, a);
                }

                float edge = hi - lo;
                return fixed4(_OutlineColor.rgb, _OutlineColor.a * edge * i.color.a);
            }
            ENDCG
        }
    }
}
