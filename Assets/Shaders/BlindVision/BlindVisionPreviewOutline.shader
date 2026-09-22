// Sef icin yerlestirme onizlemesi (GDD 4.1.2 (2)): nabiz gibi atan BEYAZ KONTUR CIZGISI — dolgu yok.
// Dunyanin konturu (BlindVisionOutline.shader) derinlik+normal tamponundan uretilir ve ekrani tamamen
// yeniden basar; yari saydam onizleme o tamponlara yazmadigi icin orada gorunmez. Bu shader
// onizlemenin KENDI geometrisini, kontur gecisinden SONRA, ayri bir gecisle cizer (URP Render Objects,
// katman filtresi = onizleme katmani). Renk tamponu okunmaz.
//
// Iki gecis (Render Objects'te overrideMaterialPassIndex ile secilir):
//   0 PreviewMask    : onizlemenin siluetini stencil'e yazar (renk yazmaz).
//   1 PreviewOutline : ayni geometriyi EKRAN UZAYINDA disari genisletip cizer, stencil'in dolu oldugu
//                      (yani onizlemenin kendi ici) pikselleri atlar -> geriye yalnizca halka kalir.
// Nabiz: cizgi kalinligi ve parlakligi zamanla atar (_PulseSpeed, _PulseAmplitude).
// Derinlik: her iki gecis de sahne derinligine gore test edilir (ZTest LEqual) — onizleme, onundeki
// nesnenin ARKASINDA kalir (GDD 4.1.2 "Hedef gorunur olmali"); kismi ortme piksel piksel dogru calisir.
// _DepthBias (metre): onizlemenin oturdugu yuzey (tezgah) halkayi kesmesin diye geometri kameraya dogru
// bu kadar kaydirilarak test edilir. Deger 0,02 — Duzeltme "Sef'in yuvadaki ince nesneleri tam gormesi"
// (22 Eyl 2026) adiminda olcumle secildi: oge pivotu tabana alindiktan sonra (artik tezgaha gomulu
// degil, ustunde duruyor) 0,01'den itibaren piksel karsilastirmasi 0,25 ile ozdesti; 0,005 yetersizdi.
// 0,02 pratik etkilesim menzilinin (PlayerInteractor.interactRange = 2,5 m) tamaminda payli.
// Not: genisletme merkezi nesnenin SINIR KUTUSU MERKEZIDIR, pivot degil — bu, pivotun (0,0,0) tabanda
// durdugu ITEM prefab'inda ayri bir "Mesh" cocugunun kendi (0,0,0) noktasinin mesh'in bounds merkezine
// denk gelmesinden (yer tutucu Cylinder/Sphere mesh'leri merkezli) DOGAL olarak saglanir — shader hicbir
// bounds hesabi yapmaz, TransformObjectToHClip(0,0,0) cagirdigi "object" zaten Mesh cocugudur.
Shader "CookNoEvil/BlindVisionPreviewOutline"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1, 1, 1, 1)
        _LineThickness ("Line Thickness (pixels)", Float) = 2
        _PulseSpeed ("Pulse Speed (Hz)", Float) = 1.5
        _PulseAmplitude ("Pulse Amplitude (0..1)", Range(0, 1)) = 0.6
        _DepthBias ("Depth Bias (meters toward camera)", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _LineColor;
            float _LineThickness;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _DepthBias;
        CBUFFER_END

        // Derinlik testi icin geometriyi kameraya dogru _DepthBias metre kaydirir: onizlemenin oturdugu
        // yuzey (ve yuzeye gomulu kisimlar) halkayi kesmez; gercek bir engel (oyuncu vb.) ise cok daha
        // onde oldugu icin onizlemeyi yine orter.
        float4 BiasedClipPosition(float3 positionOS)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float3 toCamera = normalize(GetCameraPositionWS() - positionWS);
            return TransformWorldToHClip(positionWS + toCamera * _DepthBias);
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half brightness : TEXCOORD0;
        };
        ENDHLSL

        // ---- 0: siluet maskesi (yalnizca stencil) ----
        Pass
        {
            Name "PreviewMask"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZTest LEqual
            ZWrite Off
            ColorMask 0
            Cull Off
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex VertMask
            #pragma fragment FragMask

            Varyings VertMask(Attributes input)
            {
                Varyings output;
                output.positionCS = BiasedClipPosition(input.positionOS.xyz);
                output.brightness = 0;
                return output;
            }

            half4 FragMask(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ---- 1: genisletilmis halka (maskenin disi) ----
        Pass
        {
            Name "PreviewOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZTest LEqual
            ZWrite Off
            Cull Off
            Blend Off
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline

            Varyings VertOutline(Attributes input)
            {
                float pulse = sin(_Time.y * _PulseSpeed * TWO_PI);      // -1..1
                float widthPixels = max(_LineThickness * (1.0 + _PulseAmplitude * pulse), 0.0);

                float4 clip = BiasedClipPosition(input.positionOS.xyz);
                float4 clipCenter = TransformObjectToHClip(float3(0.0, 0.0, 0.0));

                // Pivottan disari yon, piksel uzayinda; genisletme piksel cinsinden sabit kalinlik verir.
                float2 dirPixels = (clip.xy / clip.w - clipCenter.xy / clipCenter.w) * _ScaledScreenParams.xy * 0.5;
                float2 dir = dirPixels / max(length(dirPixels), 1e-5);
                clip.xy += dir * widthPixels * 2.0 / _ScaledScreenParams.xy * clip.w;

                Varyings output;
                output.positionCS = clip;
                output.brightness = (half)lerp(1.0 - _PulseAmplitude, 1.0, 0.5 + 0.5 * pulse);
                return output;
            }

            half4 FragOutline(Varyings input) : SV_Target
            {
                return half4(_LineColor.rgb * input.brightness, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
