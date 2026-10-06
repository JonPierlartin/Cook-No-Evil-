// CNE outline — maske geçişi. Outline rendering layer'ındaki nesneler bu shader'la (override material) tek kanallı bir
// dokuya çizilir: kameranın derinliğine karşı sınanır (önünde başka nesne varsa o kısım maskeye girmez), derinliğe
// yazmaz. Kenar geçişi çizgiyi yalnızca bu maskenin dokunduğu yerde uygular.
Shader "Hidden/CNE/OutlineMask"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CNEOutlineMask"

            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
