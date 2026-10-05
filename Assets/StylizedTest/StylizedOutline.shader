// DENEME — stilize görünümün kontur çizgileri: geometrinin kenarlarına ince koyu çizgi çizer.
// Kenarlar, Şef'in kör görüşüyle aynı yöntemle YALNIZCA derinlik + normal tamponlarından bulunur (yüzeydeki
// desen/doku çizgi üretmez); fark, çıktının siyah zemin yerine sahnenin ÜSTÜNE saydam olarak bindirilmesidir.
// Tam ekran tek üçgen; StylizedOutlinePass tarafından opak nesnelerden sonra çizilir.
//
// İki geçiş, stencil'in 1. bitine göre (toon shader'daki _OutlineMask yazar):
//  - Geçiş 0 (maske yok): derinlik + normal kenarları — dış hat ve iç ayrıntı çizgileri.
//  - Geçiş 1 (maske var; karakterler): yalnızca derinlik kenarları — dış hat. Parmak, göz, tuş gibi küçük ve
//    kıvrımlı ayrıntılarda normal kenarları birbirine girip modeli karartıyordu.
// Uzaktaki çizgiler 1 piksele iner (_ThinDistance).
Shader "CookNoEvil/StylizedTest/Outline"
{
    Properties
    {
        _LineColor ("Cizgi rengi", Color) = (0, 0, 0, 1)
        _LineThickness ("Cizgi kalinligi (piksel)", Float) = 2
        _DepthThreshold ("Derinlik esigi (goz derinliginin orani)", Float) = 0.05
        _NormalThreshold ("Normal esigi", Float) = 0.4
        _GrazingClamp ("Egik yuzey payi", Float) = 0.1
        _ThinDistance ("Incelme mesafesi (m)", Float) = 4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _LineColor;
            float _LineThickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _GrazingClamp;
            float _ThinDistance;
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

        // includeNormalEdges: iç ayrıntı çizgileri (yüzeyin yön değiştirdiği yerler) de çizilsin mi.
        half4 Outline(float2 uv, bool includeNormalEdges)
        {
            float rawDepth = SampleSceneDepth(uv);
            float centerDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

            // Uzakta çizgi 1 piksele iner: küçük görünen nesneleri doldurmasın.
            float thickness = centerDepth > _ThinDistance ? 1.0 : _LineThickness;
            float2 texel = 1.0 / _ScaledScreenParams.xy;
            float halfFloor = floor(thickness * 0.5);
            float halfCeil = ceil(thickness * 0.5);

            // Roberts cross: merkez etrafında iki çapraz örnek çifti.
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

            float3 centerNormal = SampleSceneNormals(uv);
            float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
            float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);
            float nDotV = saturate(dot(centerNormal, viewDirWS));
            float edge = depthEdgeValue > _DepthThreshold * centerDepth / max(nDotV, _GrazingClamp) ? 1.0 : 0.0;

            if (includeNormalEdges)
            {
                float3 dn1 = SampleSceneNormals(uvTR) - SampleSceneNormals(uvBL);
                float3 dn2 = SampleSceneNormals(uvTL) - SampleSceneNormals(uvBR);
                edge = max(edge, sqrt(dot(dn1, dn1) + dot(dn2, dn2)) > _NormalThreshold ? 1.0 : 0.0);
            }

            return half4(_LineColor.rgb, _LineColor.a * edge);
        }
        ENDHLSL

        Pass
        {
            Name "StylizedOutline"

            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return Outline(input.uv, true); }
            ENDHLSL
        }

        Pass
        {
            Name "StylizedOutlineSilhouette"

            Stencil
            {
                Ref 1
                ReadMask 1
                Comp Equal
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target { return Outline(input.uv, false); }
            ENDHLSL
        }
    }

    Fallback Off
}
