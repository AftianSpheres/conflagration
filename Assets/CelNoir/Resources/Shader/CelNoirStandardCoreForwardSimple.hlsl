/*
    CelNoir standard shader core, forward rendering path (simple)

    Part of CelNoir cel shader package.
    
    Copyright (C) 2017 Tritonic Games.
    Do not redistribute or use without permission.
    
    Based on Unity built-in shader source, copyright (c) 2016 Unity Technologies,
    used in compliance with license. (see license_unity_builtins.txt)
*/

#include "UnityStandardCoreForwardSimple.cginc"
#include "CelNoir.hlsl"

half3 BRDF3DirectSimple_CelNoir(half3 diffColor, half3 specColor, half smoothness, half rl)
{
    #if SPECULAR_HIGHLIGHTS
        return BRDF3_Direct_CelNoir(diffColor, specColor, Pow4(rl), smoothness);
    #else
        return diffColor;
    #endif
}

half4 fragForwardBaseSimpleInternal_CelNoir (VertexOutputBaseSimple i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);
    FragmentCommonData s = FragmentSetupSimple(i);
    UnityLight mainLight = MainLightSimple(i, s);
    #if !defined(LIGHTMAP_ON) && defined(_NORMALMAP)
    half ndotl = CelStep(saturate(dot(s.tangentSpaceNormal, i.tangentSpaceLightDir)));
    #else
    half ndotl = CelStep(saturate(dot(s.normalWorld, mainLight.dir)));
    #endif
    //we can't have worldpos here (not enough interpolator on SM 2.0) so no shadow fade in that case.
    half shadowMaskAttenuation = UnitySampleBakedOcclusion(i.ambientOrLightmapUV, 0);
    half realtimeShadowAttenuation = SHADOW_ATTENUATION(i);
    half atten = UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, 0);
    half occlusion = Occlusion(i.tex.xy);
    half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), LightDirForSpecular(i, mainLight));
    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
    half3 attenuatedLightColor = gi.light.color * ndotl;
    half3 c = BRDF3_Indirect_CelNoir(s.diffColor, s.specColor, gi.indirect, PerVertexGrazingTerm(i, s), PerVertexFresnelTerm(i));
    c += BRDF3DirectSimple_CelNoir(s.diffColor, s.specColor, s.smoothness, rl) * attenuatedLightColor;
    c += Emission(i.tex.xy);
    UNITY_APPLY_FOG(i.fogCoord, c);
    return OutputForward (half4(c, 1), s.alpha);
}

half4 fragForwardAddSimpleInternal_CelNoir (VertexOutputForwardAddSimple i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);
    FragmentCommonData s = FragmentSetupSimpleAdd(i);
    half3 c = BRDF3DirectSimple_CelNoir(s.diffColor, s.specColor, s.smoothness, dot(REFLECTVEC_FOR_SPECULAR(i, s), i.lightDir));
    #if SPECULAR_HIGHLIGHTS // else diffColor has premultiplied light color
        c *= _LightColor0.rgb;
    #endif
    c *= UNITY_SHADOW_ATTENUATION(i, s.posWorld) * CelStep(saturate(dot(LightSpaceNormal(i, s), i.lightDir)));
    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
    return OutputForward (half4(c, 1), s.alpha);
}