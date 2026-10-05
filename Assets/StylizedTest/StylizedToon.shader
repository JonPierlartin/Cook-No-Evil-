// DENEME — stilize (toon) aydınlatma. URP; ana ışık + ek ışıklar + gölge alma/atma + sis.
// Işık-gölge geçişi bir ramp dokusundan okunur (yumuşak geçiş); gölge siyah değil, ayarlanabilir bir renktir.
// Özellik adları URP Lit ile aynıdır (_BaseMap, _BaseColor): materyaller çalışma zamanında bu shader'a
// çevrilebilir ve MaterialPropertyBlock ile yazılan renkler (ör. köftenin pişmişlik rengi) çalışmaya devam eder.
// Gölge atma ve derinlik/normal geçişleri URP Lit'ten alınır (SSAO ve Şef'in kontur görüşü DepthNormals ister).
Shader "CookNoEvil/StylizedTest/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Doku", 2D) = "white" {}
        [MainColor] _BaseColor ("Renk", Color) = (1, 1, 1, 1)

        [Header(Isik ve golge)]
        [NoScaleOffset] _RampTex ("Ramp (sol = golge, sag = isik)", 2D) = "white" {}
        _ShadowColor ("Golge rengi", Color) = (0.45, 0.40, 0.62, 1)
        _AmbientStrength ("Ortam isigi gucu (golge tarafi)", Range(0, 2)) = 0.6
        _LitBrightness ("Isik tarafi parlakligi", Range(0.5, 1.5)) = 1

        [Header(Kenar parlamasi)]
        _RimColor ("Kenar rengi", Color) = (1, 0.93, 0.78, 1)
        _RimIntensity ("Kenar siddeti", Range(0, 2)) = 0.35
        _RimPower ("Kenar inceligi", Range(0.5, 8)) = 3

        // URP Lit geçişlerinin (ShadowCaster / DepthOnly / DepthNormals) beklediği özellikler.
        [HideInInspector] _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _BumpMap ("Normal Map", 2D) = "bump" {}
        [HideInInspector] _BumpScale ("Normal Scale", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex);
            SAMPLER(sampler_linear_clamp);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half _AmbientStrength;
                half _LitBrightness;
                half4 _RimColor;
                half _RimIntensity;
                half _RimPower;
                half _Cutoff;
                half _BumpScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // Ramp: 0 = tam gölge, 1 = tam ışık.
            half Ramp(half amount)
            {
                return SAMPLE_TEXTURE2D(_RampTex, sampler_linear_clamp, float2(saturate(amount), 0.5)).r;
            }

            // Işığın yalnızca TONU alınır: şiddeti 1'in üstündeyse renkler yanmasın diye en parlak kanal 1'e indirilir
            // (toon görünümde parlaklığı ışığın şiddeti değil, ramp ve _LitBrightness belirler).
            half3 LightTint(half3 lightColor)
            {
                return lightColor / max(1.0, max(lightColor.r, max(lightColor.g, lightColor.b)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalWS = normalize(input.normalWS);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                // Ana ışık: yarım-Lambert (yumuşak dönüş) × gölge; ramp'ten okunur.
                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half mainAmount = (dot(normalWS, mainLight.direction) * 0.5 + 0.5) * mainLight.shadowAttenuation;
                // Gölge tarafı: gölge rengi + ortam ışığının (Environment Lighting; gradient ise gökyüzü/ufuk/zemin) payı.
                half3 shadowSide = _ShadowColor.rgb + SampleSH(normalWS) * (_AmbientStrength * 0.25);
                half3 litSide = LightTint(mainLight.color) * _LitBrightness;
                half3 lighting = lerp(shadowSide, litSide, Ramp(mainAmount));

                // Ek ışıklar: aynı ramp, ışığın kendi rengi ve mesafe sönümüyle.
                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half amount = saturate(dot(normalWS, light.direction)) * light.shadowAttenuation;
                    lighting += Ramp(amount) * LightTint(light.color) * saturate(light.distanceAttenuation);
                LIGHT_LOOP_END
                #endif

                half3 color = base.rgb * lighting;

                // Kenar parlaması: bakışa yan duran yüzeylerde, yalnızca ışık alan tarafta.
                half rim = pow(1.0 - saturate(dot(normalWS, viewWS)), _RimPower) * _RimIntensity;
                color += _RimColor.rgb * rim * saturate(mainAmount * 2.0);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}
