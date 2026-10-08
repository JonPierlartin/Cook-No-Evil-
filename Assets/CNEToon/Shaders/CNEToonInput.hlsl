#ifndef CNE_TOON_INPUT_INCLUDED
#define CNE_TOON_INPUT_INCLUDED

// CNE/Toon — tüm geçişlerin ortak girdileri. SRP Batcher için bütün materyal özellikleri TEK bir UnityPerMaterial
// bloğunda ve her geçişte aynı düzende durur; yeni özellik eklenirse buraya eklenir.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half _VertexAOStrength;
    half _Wrap;
    half _ShadowThreshold;
    half _ShadowSoftness;
    half _CastShadowSoftness;
    half _MidThreshold;
    half _MidStrength;
    half4 _ShadowTint;
    half _ShadowStrength;
    half _GIStrength;
    half _LightClamp;
    half _LightTint;
    half4 _SpecColor;
    half _SpecSize;
    half _SpecSoftness;
    half _MatCapStrength;
    half4 _RimColor;
    half _RimThreshold;
    half _RimSoftness;
    half _RimStrength;
    half _RimUpBias;
    half4 _EmissionColor;
    half _EmissionBaseTint;
    half _HatchStrength;
    half _HatchScale;
    half4 _PatternColor;
    float4 _PatternSize;
    half _PatternType;
    half _PatternLine;
CBUFFER_END

TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
TEXTURE2D(_PropMap);    SAMPLER(sampler_PropMap);
TEXTURE2D(_MatCapTex);  SAMPLER(sampler_MatCapTex);
TEXTURE2D(_HatchTex);   SAMPLER(sampler_HatchTex);

// Palet rengi × renderer başına renk (_BaseColor MaterialPropertyBlock ile yazılabilir: pişme ve sinyal rengi).
half4 CNESampleBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
}

void CNEAlphaClip(half alpha)
{
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
}

#endif
