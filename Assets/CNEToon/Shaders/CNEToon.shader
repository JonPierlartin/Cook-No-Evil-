// CNE/Toon — Cook No Evil! toon / cel-shaded yüzey shader'ı (URP, el yazımı HLSL). Ayrıntı: docs/ToonShader.md.
//
// Değişmez kurallar (bozulursa Şef'in kontur görüşü bozulur — GDD 4.1.1, 5.6):
//  - DepthOnly ve DepthNormals geçişleri vardır; DepthNormals YALNIZCA geometrik (vertex) normal yazar.
//  - Normal map, metalik, pürüzlülük, AO dokusu YOKTUR ve eklenmez.
//  - Bütün geçişler aynı vertex konumunu kullanır; vertex kaydırma yoktur.
//  - Oyun durumu yalnızca renkle anlatılır (_BaseColor, renderer başına yazılabilir).
Shader "CNE/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palet dokusu", 2D) = "white" {}
        [MainColor] _BaseColor ("Renk", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _PropMap ("Ozellik maskesi (R parlama, G emission, B desen)", 2D) = "black" {}
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha clip", Float) = 0
        _Cutoff ("Alpha esigi", Range(0, 1)) = 0.5

        _Wrap ("Isik sarmasi", Range(0, 1)) = 0.5
        _ShadowThreshold ("Golge esigi", Range(0, 1)) = 0.5
        _ShadowSoftness ("Golge gecis yumusakligi", Range(0.001, 0.5)) = 0.03
        [Toggle(_MIDBAND_ON)] _MidBand ("Ara bant", Float) = 0
        _MidThreshold ("Ara bant esigi", Range(0, 1)) = 0.75
        _MidStrength ("Ara bant koyulugu", Range(0, 1)) = 0.35
        _ShadowTint ("Golge rengi", Color) = (0.243, 0.290, 0.478, 1)
        _ShadowStrength ("Golge gucu", Range(0, 1)) = 0.55
        _GIStrength ("Dolayli isik gucu", Range(0, 2)) = 0.5
        _LightClamp ("Isik ust siniri", Range(1, 3)) = 1.2

        [Toggle(_SPECULAR_ON)] _Specular ("Parilti", Float) = 0
        _SpecColor ("Parilti rengi", Color) = (1, 1, 1, 1)
        _SpecSize ("Parilti boyutu", Range(0.001, 0.5)) = 0.05
        _SpecSoftness ("Parilti kenar yumusakligi", Range(0.0005, 0.1)) = 0.005
        [Toggle(_MATCAP_ON)] _MatCap ("Matcap", Float) = 0
        [NoScaleOffset] _MatCapTex ("Matcap dokusu", 2D) = "gray" {}
        _MatCapStrength ("Matcap gucu", Range(0, 1)) = 1

        [Toggle(_RIM_ON)] _Rim ("Kenar isigi", Float) = 0
        _RimColor ("Kenar isigi rengi", Color) = (1, 1, 1, 1)
        _RimThreshold ("Kenar isigi esigi", Range(0, 1)) = 0.6
        _RimSoftness ("Kenar isigi yumusakligi", Range(0.001, 0.5)) = 0.03
        _RimStrength ("Kenar isigi gucu", Range(0, 2)) = 0.5
        _RimUpBias ("Yukari bakan yuzey agirligi", Range(0, 1)) = 0.5

        [Toggle(_EMISSION_ON)] _Emission ("Emission", Float) = 0
        [HDR] _EmissionColor ("Emission rengi", Color) = (0, 0, 0, 1)

        [Toggle(_HATCH_ON)] _Hatch ("Tarama deseni", Float) = 0
        [NoScaleOffset] _HatchTex ("Tarama dokusu", 2D) = "white" {}
        _HatchStrength ("Tarama gucu", Range(0, 1)) = 0.2
        _HatchScale ("Tarama olcegi (1/m)", Float) = 4

        _VertexAOStrength ("Vertex AO gucu", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ForwardVertex
            #pragma fragment ForwardFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _MIDBAND_ON
            #pragma shader_feature_local_fragment _SPECULAR_ON
            #pragma shader_feature_local_fragment _MATCAP_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local _HATCH_ON

            // URP anahtar kelimeleri — Lit.shader (URP 17.5) ForwardLit geçişinden.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing

            #include "CNEToonInput.hlsl"
            #include "CNEToonForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "CNEToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ShadowUtils.SetupShadowCasterConstantBuffer yazar (URP ShadowCasterPass.hlsl ile aynı).
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                // Gölge payı yalnızca gölge haritasına uygulanır; kameranın gördüğü konum değişmez.
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                CNEAlphaClip(CNESampleBase(input.uv).a);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "CNEToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half ShadowlessDepth(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                CNEAlphaClip(CNESampleBase(input.uv).a);
                return input.positionCS.z;
            }

            half DepthFragment(Varyings input) : SV_Target
            {
                return ShadowlessDepth(input);
            }
            ENDHLSL
        }

        // Şef'in kontur görüşü ve yeni outline bu geçişin yazdığı normalleri okur: YALNIZCA geometrik normal.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing

            #include "CNEToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            void DepthNormalsFragment(
                Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                CNEAlphaClip(CNESampleBase(input.uv).a);

                // URP DepthNormalsPass.hlsl ile aynı kodlama.
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(input.normalWS);
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                outNormalWS = half4(PackFloat2To888(remappedOctNormalWS), 0.0);
            #else
                outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            #endif

            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        // Lightmap bake'i: yüzeyin rengini ve emission'ını ışık haritası hesaplayıcısına bildirir.
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UniversalVertexMeta
            #pragma fragment CNEMetaFragment
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "CNEToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

            half4 CNEMetaFragment(Varyings input) : SV_Target
            {
                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = CNESampleBase(input.uv).rgb;
            #if defined(_EMISSION_ON)
                metaInput.Emission = _EmissionColor.rgb * SAMPLE_TEXTURE2D(_PropMap, sampler_PropMap, input.uv).g;
            #endif
                return UniversalFragmentMeta(input, metaInput);
            }
            ENDHLSL
        }
    }

    CustomEditor "CNEToonShaderGUI"
    Fallback Off
}
