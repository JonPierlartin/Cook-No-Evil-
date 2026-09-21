// Sef'in kor gorusu (GDD 4.1.1): siyah zemin uzerinde beyaz kontur cizgileri. Dolgu, renk, golge,
// doku YOK — cikti yalnizca "zemin rengi" ile "cizgi rengi" arasinda bir karisimdir.
//
// K2 (CLAUDE.md): kenarlar YALNIZCA derinlik + normal tamponlarindan uretilir (3B bicimden).
// Hicbir renk tamponu (blit kaynagi, opak dokusu, sahne rengi) bu dosyada ornek alinmaz ve hatta
// tanimlanmaz; vertex asamasi da blit kaynagina bagli olmasin diye kendi tam ekran ucgenini
// SV_VertexID'den uretir. Boylece yuzeye basili desen/etiket kenar olarak GORUNMEZ.
//
// Full Screen Pass Renderer Feature ile kullanilir: Requirements = Depth + Normal,
// Fetch Color Buffer = KAPALI (renk tamponu kopyalanmaz bile).
Shader "CookNoEvil/BlindVisionOutline"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _LineThickness ("Line Thickness (pixels)", Float) = 1
        _DepthThreshold ("Depth Threshold (fraction of eye depth)", Float) = 0.05
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _GrazingClamp ("Grazing Clamp (min n.v used to relax the depth threshold)", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "BlindVisionOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LineColor;
                half4 _BackgroundColor;
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

                // Roberts cross: merkez etrafinda iki capraz ornek cifti.
                float2 uv = input.uv;
                float2 uvBL = uv - texel * halfFloor;
                float2 uvTR = uv + texel * halfCeil;
                float2 uvBR = uv + float2(texel.x * halfCeil, -texel.y * halfFloor);
                float2 uvTL = uv + float2(-texel.x * halfFloor, texel.y * halfCeil);

                // ---- Derinlik kenari (yalnizca _CameraDepthTexture) ----
                float dBL = EyeDepth(uvBL);
                float dTR = EyeDepth(uvTR);
                float dBR = EyeDepth(uvBR);
                float dTL = EyeDepth(uvTL);
                float depthEdgeValue = sqrt((dTR - dBL) * (dTR - dBL) + (dTL - dBR) * (dTL - dBR));

                // Egik (grazing) duzlemlerde bir piksel adiminda derinlik zaten hizla degisir; esik,
                // merkezdeki yuzey normali ile bakis yonu arasindaki aciya gore gevsetilir.
                float rawDepth = SampleSceneDepth(uv);
                float centerDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 centerNormal = SampleSceneNormals(uv);
                float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);
                float nDotV = saturate(dot(centerNormal, viewDirWS));
                float depthEdge = depthEdgeValue > _DepthThreshold * centerDepth / max(nDotV, _GrazingClamp) ? 1.0 : 0.0;

                // ---- Normal kenari (yalnizca _CameraNormalsTexture) ----
                float3 nBL = SampleSceneNormals(uvBL);
                float3 nTR = SampleSceneNormals(uvTR);
                float3 nBR = SampleSceneNormals(uvBR);
                float3 nTL = SampleSceneNormals(uvTL);
                float3 dn1 = nTR - nBL;
                float3 dn2 = nTL - nBR;
                float normalEdgeValue = sqrt(dot(dn1, dn1) + dot(dn2, dn2));
                float normalEdge = normalEdgeValue > _NormalThreshold ? 1.0 : 0.0;

                float edge = max(depthEdge, normalEdge);
                return lerp(_BackgroundColor, _LineColor, edge);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
