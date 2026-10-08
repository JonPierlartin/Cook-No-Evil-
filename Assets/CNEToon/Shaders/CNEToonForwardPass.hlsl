#ifndef CNE_TOON_FORWARD_PASS_INCLUDED
#define CNE_TOON_FORWARD_PASS_INCLUDED

// CNE/Toon ileri (forward) geçişi. Girdi kurulumu, GI ve ek ışık döngüsü URP 17.5'in SimpleLitForwardPass.hlsl ve
// Lighting.hlsl dosyalarındaki yolu izler (Forward ve Forward+ aynı makrolarla); yalnızca ışığın yüzeye nasıl
// uygulandığı farklıdır (basamaklı).
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half fogFactor : TEXCOORD3;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD4;
#endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#ifdef DYNAMICLIGHTMAP_ON
    float2 dynamicLightmapUV : TEXCOORD6;
#endif
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD7;
#endif
    half vertexAO : TEXCOORD8;
#if defined(_HATCH_ON)
    float3 positionOS : TEXCOORD9;
    half3 normalOS : TEXCOORD10;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ForwardVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;
    output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));

#if defined(_FOG_FRAGMENT)
    output.fogFactor = 0;
#else
    output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    // Vertex rengi R = Blender'da pişirilmiş AO. Renk akışı olmayan mesh'te Unity beyaz verir (AO yok).
    output.vertexAO = input.color.r;

#if defined(_HATCH_ON)
    output.positionOS = input.positionOS.xyz;
    output.normalOS = input.normalOS;
#endif
    return output;
}

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactor);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    // Lightmap / light probe / APV — SimpleLitForwardPass.hlsl InitializeBakedGIData ile aynı yol.
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif
}

// Işığın yüzeye düşen miktarı → bant (0 = gölge tonu, 1 = tam ışık). Gölgedeki yüzey ile ışığa dönük olmayan yüzey
// aynı tonu alır.
// Yüzeyin yönü ve düşen gölge AYRI basamaklanır, sonra birleştirilir: ikisi çarpılıp tek eşikten geçirilirse düşen
// gölgenin kenarı yüzeyin açısına göre kayar ve gölge haritasının pikselleri testere dişi gibi görünür. Düşen
// gölgenin kenarı kendi yumuşaklığıyla (_CastShadowSoftness) geçer; yumuşak gölge filtresinin 0–1 geçişinin ortası
// alındığı için kenar gölge haritasının piksellerini değil, filtrelenmiş çizgiyi izler.
// Işığın rengi: _LightTint 1 iken olduğu gibi, 0 iken yalnızca şiddeti (renksiz; en parlak kanal). Mimaride düşük
// tutulur ki duvar ve zemin palet rengini korusun (sıcak güneş açık renkli duvarı turuncuya çekmesin): şiddeti 1
// olan renkli bir ışığın altında ışık alan yüzey tam palet rengindedir.
half3 CNELightColor(half3 color)
{
    half peak = max(color.r, max(color.g, color.b));
    return lerp(peak.xxx, color, _LightTint);
}

half CNEBand(half nDotL, half shadow)
{
    half facing = saturate(nDotL * (1.0 - _Wrap) + _Wrap);
    half band = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, facing);
#if defined(_MIDBAND_ON)
    half mid = smoothstep(_MidThreshold - _ShadowSoftness, _MidThreshold + _ShadowSoftness, facing);
    band *= lerp(1.0 - _MidStrength, 1.0, mid);
#endif
    half cast = smoothstep(0.5 - _CastShadowSoftness, 0.5 + _CastShadowSoftness, shadow);
    return min(band, cast);
}

#if defined(_SPECULAR_ON)
half CNESpecular(half3 normalWS, half3 lightDirWS, half3 viewDirWS)
{
    half nDotH = saturate(dot(normalWS, SafeNormalize(lightDirWS + viewDirWS)));
    half edge = 1.0 - _SpecSize;
    return smoothstep(edge - _SpecSoftness, edge + _SpecSoftness, nDotH);
}
#endif

#if defined(_HATCH_ON)
// Obje uzayında üç eksenli (triplanar) örnekleme: desen nesneye yapışır, kamera dönünce kaymaz.
half CNEHatch(float3 positionOS, half3 normalOS)
{
    half3 weights = abs(normalize(normalOS));
    weights /= (weights.x + weights.y + weights.z);
    float3 p = positionOS * _HatchScale;
    half x = SAMPLE_TEXTURE2D(_HatchTex, sampler_HatchTex, p.zy).r;
    half y = SAMPLE_TEXTURE2D(_HatchTex, sampler_HatchTex, p.xz).r;
    half z = SAMPLE_TEXTURE2D(_HatchTex, sampler_HatchTex, p.xy).r;
    return x * weights.x + y * weights.y + z * weights.z;
}
#endif

#if defined(_SURFACEPATTERN_ON)
// Dünya uzayında yüzey deseni: mimaride UV yoktur, desen yüzeyin baktığı eksene göre dünya konumundan üretilir.
// Dönen değer 0–1: yüzey rengi ile desen rengi (_PatternColor) arasındaki karışım. Yalnızca rengi değiştirir.
// Türler: 0 fayans (şaşırtmalı derz), 1 kare (düz derz), 2 şerit (yalnızca düşey derz), 3 dama.
half CNESurfacePattern(float3 positionWS, half3 normalWS)
{
    half3 axis = abs(normalWS);
    float2 p = axis.y > max(axis.x, axis.z) ? positionWS.xz
        : (axis.x > axis.z ? positionWS.zy : positionWS.xy);
    float2 cell = p / max(_PatternSize.xy, 1e-4);

    if (_PatternType > 2.5)
    {
        // Dama: kenarlar piksel genişliğinde yumuşatılır.
        float2 tri = abs(frac(cell * 0.5) - 0.5) * 2.0;
        float2 width = max(fwidth(cell), 1e-5);
        float2 side = smoothstep(0.5 - width, 0.5 + width, tri);
        return side.x * (1.0 - side.y) + side.y * (1.0 - side.x);
    }

    if (_PatternType < 0.5)
        cell.x += 0.5 * fmod(floor(cell.y), 2.0);

    // Derze uzaklık (metre).
    float2 edge = min(frac(cell), 1.0 - frac(cell)) * _PatternSize.xy;
    float gap = _PatternType > 1.5 ? edge.x : min(edge.x, edge.y);
    float pixel = max(fwidth(gap), 1e-5);
    half lineMask = 1.0 - smoothstep(_PatternLine * 0.5 - pixel, _PatternLine * 0.5 + pixel, gap);
    // Derz bir pikselden inceyken (uzakta) kırpışmasın diye solar.
    return lineMask * saturate(_PatternLine / (pixel * 1.5));
}
#endif

void ForwardFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 base = CNESampleBase(input.uv);
    CNEAlphaClip(base.a);
#if defined(_SURFACEPATTERN_ON)
    base.rgb = lerp(base.rgb, _PatternColor.rgb, CNESurfacePattern(input.positionWS, input.normalWS) * _PatternColor.a);
#endif

#if defined(_SPECULAR_ON) || defined(_MATCAP_ON) || defined(_EMISSION_ON) || defined(_HATCH_ON)
    half3 props = SAMPLE_TEXTURE2D(_PropMap, sampler_PropMap, input.uv).rgb;
#endif

    InputData inputData;
    InitializeInputData(input, inputData);
    half3 normalWS = inputData.normalWS;
    half3 viewDirWS = inputData.viewDirectionWS;

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    MixRealtimeAndBakedGI(mainLight, normalWS, inputData.bakedGI);

    half3 shadowMultiplier = lerp(half3(1, 1, 1), _ShadowTint.rgb, _ShadowStrength);

    // light: yüzey rengiyle çarpılacak toplam ışık. mainBand: ana ışığın bandı (tarama deseni bunu kullanır).
    half3 light = 0;
    half3 specular = 0;
    half mainBand = 0;

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        mainBand = CNEBand(dot(normalWS, mainLight.direction), mainLight.shadowAttenuation * mainLight.distanceAttenuation);
        light += CNELightColor(mainLight.color) * lerp(shadowMultiplier, half3(1, 1, 1), mainBand);
    #if defined(_SPECULAR_ON)
        specular += mainLight.color * (CNESpecular(normalWS, mainLight.direction, viewDirWS) * mainBand);
    #endif
    }

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light2 = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light2.layerMask, meshRenderingLayers))
    #endif
        {
            half band = CNEBand(dot(normalWS, light2.direction), light2.shadowAttenuation);
            light += CNELightColor(light2.color) * (band * light2.distanceAttenuation);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light2 = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light2.layerMask, meshRenderingLayers))
    #endif
        {
            // Bant yüzeyin yönünden ve gölgeden, parlaklık mesafeden gelir: lamba uzaklaştıkça bant kaymaz, solar.
            half band = CNEBand(dot(normalWS, light2.direction), light2.shadowAttenuation);
            light += CNELightColor(light2.color) * (band * light2.distanceAttenuation);
        #if defined(_SPECULAR_ON)
            specular += light2.color * (CNESpecular(normalWS, light2.direction, viewDirWS) * band * light2.distanceAttenuation);
        #endif
        }
    LIGHT_LOOP_END
#endif

    // Dolaylı ışık basamaksız eklenir; toplam sınırlanır ki lambaların yanında palet renkleri patlamasın.
    light += inputData.bakedGI * _GIStrength;
    light = min(light, _LightClamp);
    light *= lerp(1.0, input.vertexAO, _VertexAOStrength);

    half3 color = base.rgb * light;

#if defined(_HATCH_ON)
    half hatch = CNEHatch(input.positionOS, input.normalOS);
    color *= lerp(1.0, hatch, _HatchStrength * props.b * (1.0 - mainBand));
#endif

#if defined(_MATCAP_ON)
    half2 matcapUV = TransformWorldToViewNormal(normalWS).xy * 0.5 + 0.5;
    half3 matcap = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, matcapUV).rgb;
    color = lerp(color, matcap, props.r * _MatCapStrength);
#endif

#if defined(_SPECULAR_ON)
    color += specular * _SpecColor.rgb * props.r;
#endif

#if defined(_RIM_ON)
    half fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
    half rim = smoothstep(_RimThreshold - _RimSoftness, _RimThreshold + _RimSoftness, fresnel);
    rim *= lerp(1.0 - _RimUpBias, 1.0, saturate(normalWS.y * 0.5 + 0.5));
    color += _RimColor.rgb * (rim * _RimStrength);
#endif

#if defined(_EMISSION_ON)
    // _EmissionBaseTint 1 iken yüzey kendi palet renginde ışır (emission rengi yalnızca şiddeti verir).
    color += _EmissionColor.rgb * lerp(half3(1, 1, 1), base.rgb, _EmissionBaseTint) * props.g;
#endif

    color = MixFog(color, inputData.fogCoord);
    outColor = half4(color, 1.0);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
