// CNE outline — kenar bulma ve bindirme. Tam ekran tek üçgen. Kenarlar YALNIZCA derinlik + normal dokularından
// bulunur (yüzeydeki renk/desen çizgi üretmez); çizgi yalnızca maskenin (Outline katmanındaki nesneler) dokunduğu
// yerde çizilir. Değerleri CNEOutlineFeature yazar; materyal elle düzenlenmez.
//
// Bindirme tek geçişte, sahne rengi kopyalanmadan yapılır (Blend One SrcAlpha):
//   sonuç = çizgiRengi × karışım × kenar  +  sahne × (1 − kenar + kenar × koyulaştırma × (1 − karışım))
// karışım 1 → düz çizgi rengi; karışım 0 → sahnenin kendi renginin koyu tonu.
Shader "Hidden/CNE/OutlineComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CNEOutlineComposite"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One SrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D(_CNEOutlineMask);

            half4 _OutlineColor;
            half _OutlineDarken;
            half _OutlineColorBlend;
            float _OutlineWidthPx;
            float _ReferenceHeight;
            float _DepthThreshold;
            float _NormalThreshold;
            float _EdgeSoftness;
            float _FadeStart;
            float _FadeEnd;

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

            // R = tam çizgi, G = yalnızca siluet.
            half2 Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_CNEOutlineMask, sampler_LinearClamp, uv, 0).rg;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float centerDepth = EyeDepth(uv);

                // Kalınlık referans yükseklikte (1080p) piksel cinsindendir; ekran yüksekliğiyle ölçeklenir ki her
                // çözünürlükte ekranın aynı oranını kaplasın.
                float2 texel = 1.0 / _ScaledScreenParams.xy;
                float widthPx = _OutlineWidthPx * _ScaledScreenParams.y / _ReferenceHeight;
                float halfWidth = max(widthPx * 0.5, 0.5);

                // Roberts cross: merkezin çevresinde iki çapraz örnek çifti.
                float2 uvBL = uv + texel * float2(-halfWidth, -halfWidth);
                float2 uvTR = uv + texel * float2(halfWidth, halfWidth);
                float2 uvBR = uv + texel * float2(halfWidth, -halfWidth);
                float2 uvTL = uv + texel * float2(-halfWidth, halfWidth);

                float dBL = EyeDepth(uvBL);
                float dTR = EyeDepth(uvTR);
                float dBR = EyeDepth(uvBR);
                float dTL = EyeDepth(uvTL);
                float nearest = min(min(min(dBL, dTR), min(dBR, dTL)), centerDepth);

                // Derinlik kenarı mesafeyle orantılı eşiklenir; eğik yüzeyde derinlik zaten hızlı değiştiği için eşik
                // bakış açısına göre gevşer (yoksa zemine sıyırarak bakınca her yer çizgi olur).
                float depthEdge = sqrt((dTR - dBL) * (dTR - dBL) + (dTL - dBR) * (dTL - dBR)) / nearest;
                float3 centerNormal = SampleSceneNormals(uv);
                float3 positionWS = ComputeWorldSpacePosition(uv, SampleSceneDepth(uv), UNITY_MATRIX_I_VP);
                float nDotV = saturate(dot(centerNormal, normalize(GetCameraPositionWS() - positionWS)));
                float depthThreshold = _DepthThreshold / max(nDotV, 0.1);

                float3 n1 = SampleSceneNormals(uvTR) - SampleSceneNormals(uvBL);
                float3 n2 = SampleSceneNormals(uvTL) - SampleSceneNormals(uvBR);
                float normalEdge = sqrt(dot(n1, n1) + dot(n2, n2));

                float silhouette = smoothstep(depthThreshold, depthThreshold * (1.0 + _EdgeSoftness), depthEdge);
                float crease = smoothstep(_NormalThreshold, _NormalThreshold * (1.0 + _EdgeSoftness), normalEdge);

                // Çizgi yalnızca maskedeki bir nesneye dokunuyorsa çizilir; yalnızca mimariye ait kenarlar maskede
                // olmadığı için atlanır. Tam çizgi alan nesnede siluet + iç kırımlar; yalnızca siluet alan nesnede
                // (karakterler: parmak, göz, tuş gibi küçük ayrıntıların iç çizgileri modeli karartır) yalnızca siluet.
                half2 mask = max(max(max(Mask(uvBL), Mask(uvTR)), max(Mask(uvBR), Mask(uvTL))), Mask(uv));
                float edge = max(max(silhouette, crease) * mask.r, silhouette * mask.g);

                // Uzakta çizgi solar.
                edge *= 1.0 - smoothstep(_FadeStart, _FadeEnd, nearest);

                half3 lineColor = _OutlineColor.rgb * (_OutlineColorBlend * edge);
                half sceneFactor = 1.0 - edge + edge * _OutlineDarken * (1.0 - _OutlineColorBlend);
                return half4(lineColor, sceneFactor);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
