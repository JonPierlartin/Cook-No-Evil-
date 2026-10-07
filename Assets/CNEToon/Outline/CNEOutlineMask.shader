// CNE outline — maske geçişi. Outline rendering layer'larındaki nesneler bu shader'la (override material) iki kanallı
// bir dokuya çizilir: R = tam çizgi alan nesneler (siluet + iç kırımlar), G = yalnızca siluet alan nesneler
// (karakterler). Kameranın derinliğine karşı sınanır (önünde başka nesne varsa o kısım maskeye girmez), derinliğe
// yazmaz. Kanallar birbirini ezmesin diye Max ile karışır. Hangi kanala yazılacağını CNEOutlineFeature verir.
// B kanalına NESNE KİMLİĞİ yazılır (renderer'ın dünya konumundan türetilen 1–255 arası bir değer): yan yana duran
// iki nesnenin, ya da bir eşyanın ayrı parçasının (kapak, sepet, kapı kanadı) sınırı buradan bulunur.
Shader "Hidden/CNE/OutlineMask"
{
    Properties
    {
        // CNEOutlineFeature yazar: (1,0,0,0) = tam çizgi kanalı, (0,1,0,0) = siluet kanalı.
        [HideInInspector] _MaskChannels ("Maske kanallari", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CNEOutlineMask"

            ZWrite Off
            ZTest LEqual
            Cull Back
            BlendOp Max
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MaskChannels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half objectId : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // Renderer'ın pivotunun dünya konumundan kimlik. 0 "maskede yok" demek olduğu için 1–255 aralığı.
                float3 pivotWS = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float hash = frac(sin(dot(pivotWS, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                output.objectId = (floor(hash * 254.0) + 1.0) / 255.0;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(_MaskChannels.rg, input.objectId, 0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
