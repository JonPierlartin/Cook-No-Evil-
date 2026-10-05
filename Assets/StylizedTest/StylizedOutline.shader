// DENEME — stilize görünümün kontur çizgileri: geometrinin kenarlarına ince koyu çizgi çizer.
// Kenarlar, Şef'in kör görüşüyle aynı yöntemle YALNIZCA derinlik + normal tamponlarından bulunur (yüzeydeki
// desen/doku çizgi üretmez); fark, çıktının siyah zemin yerine sahnenin ÜSTÜNE saydam olarak bindirilmesidir.
// Tam ekran tek üçgen; StylizedOutlinePass tarafından opak nesnelerden sonra çizilir.
Shader "CookNoEvil/StylizedTest/Outline"
{
    Properties
    {
        _LineColor ("Cizgi rengi", Color) = (0, 0, 0, 1)
        _LineThickness ("Cizgi kalinligi (piksel)", Float) = 2
        _DepthThreshold ("Derinlik esigi (goz derinliginin orani)", Float) = 0.05
        _NormalThreshold ("Normal esigi", Float) = 0.4
        _GrazingClamp ("Egik yuzey payi", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "StylizedOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LineColor;
                float _LineThickness;
                float _DepthThreshold;
                float _NormalThreshold;
                float _GrazingClamp;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texel = 1.0 / _ScaledScreenParams.xy;
                float halfFloor = floor(_LineThickness * 0.5);
                float halfCeil = ceil(_LineThickness * 0.5);

                // Roberts cross: merkez etrafında iki çapraz örnek çifti.
                float2 uv = input.uv;
                float2 uvBL = uv - texel * halfFloor;
                float2 uvTR = uv + texel * halfCeil;
                float2 uvBR = uv + float2(texel.x * halfCeil, -texel.y * halfFloor);
                float2 uvTL = uv + float2(-texel.x * halfFloor, texel.y * halfCeil);

                // Derinlik kenarı: eğik yüzeylerde derinlik zaten hızlı değiştiği için eşik bakış açısına göre gevşer.
                float dBL = EyeDepth(uvBL);
                float dTR = EyeDepth(uvTR);
                float dBR = EyeDepth(uvBR);
                float dTL = EyeDepth(uvTL);
                float depthEdgeValue = sqrt((dTR - dBL) * (dTR - dBL) + (dTL - dBR) * (dTL - dBR));

                float rawDepth = SampleSceneDepth(uv);
                float centerDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 centerNormal = SampleSceneNormals(uv);
                float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);
                float nDotV = saturate(dot(centerNormal, viewDirWS));
                float depthEdge = depthEdgeValue > _DepthThreshold * centerDepth / max(nDotV, _GrazingClamp) ? 1.0 : 0.0;

                // Normal kenarı: yüzeyin yön değiştirdiği yerler (köşeler, kıvrımlar).
                float3 dn1 = SampleSceneNormals(uvTR) - SampleSceneNormals(uvBL);
                float3 dn2 = SampleSceneNormals(uvTL) - SampleSceneNormals(uvBR);
                float normalEdge = sqrt(dot(dn1, dn1) + dot(dn2, dn2)) > _NormalThreshold ? 1.0 : 0.0;

                float edge = max(depthEdge, normalEdge);
                return half4(_LineColor.rgb, _LineColor.a * edge);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
