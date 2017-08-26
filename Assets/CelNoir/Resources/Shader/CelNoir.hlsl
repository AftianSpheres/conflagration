/*
    CelNoir shader utilities

    Part of CelNoir cel shader package.
    
    Copyright (C) 2017 Tritonic Games.
    Do not redistribute or use without permission.
    
    Based on Unity built-in shader source, copyright (c) 2016 Unity Technologies,
    used in compliance with license. (see license_unity_builtins.txt)
*/

#include "UnityStandardConfig.cginc"
#include "UnityStandardBRDF.cginc"
#include "CelNoirConfig.hlsl"

inline half CelCrush(half c)
{
    return c * step(CELCRUSH_THRESHOLD, c);
}

inline half3 CelCrush (half3 c) 
{
	return c * step(CELCRUSH_THRESHOLD, Luminance(c));
}

inline half CelStep(half c)
{
    // This is hardcoded in order to minimize unnecessary math.
    // If you want more than eight CelSteps for some reason,
    // though, it's step(1/CELSTEPS, c) * (ceil(c * CELSTEPS/2) / CELSTEPS/2)
    #if CELSTEPS == 2
        return step(0.5, c) * (ceil(c)/ 1.0);
    #elif CELSTEPS == 3
        return step(0.375, c) * (ceil(c * 1.5) / 1.5);
    #elif CELSTEPS == 4
        return step(0.25, c) * (ceil(c * 2.0) / 2.0);
    #elif CELSTEPS == 5
        return step(0.21875, c) * (ceil(c * 2.5) / 2.5);
    #elif CELSTEPS == 6
        return step(0.1875, c) * (ceil(c * 3.0) / 3.0);
    #elif CELSTEPS == 7
        return step(0.15625, c) * (ceil(c * 3.5) / 3.5);
    #elif CELSTEPS == 8
        return step(0.125, c) * (ceil(c * 4.0) / 4.0);
    #else
        return c;
    #endif
}

inline half3 CelStep(half3 c)
{
    const float Epsilon = 1e-10;
    half l = Luminance(c);
    return ((c / (l + (Epsilon * step(l, Epsilon)))) * CelStep(l));
}

half3 BDRF2_CelNoir_Internal (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi) 
{
    half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);
    half nl = CelStep(saturate(dot(normal, light.dir)));
    half nh = saturate(dot(normal, halfDir));
    half nv = saturate(dot(normal, viewDir));
    half lh = saturate(dot(light.dir, halfDir));
    // Specular term
    half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	#if UNITY_BRDF_GGX
		// GGX Distribution multiplied by combined approximation of Visibility and Fresnel
		// See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
		// https://community.arm.com/events/1155
		half a = roughness;
		half a2 = a*a;
		half d = nh * nh * (a2 - 1.h) + 1.00001h;
	#ifdef UNITY_COLORSPACE_GAMMA
		// Tighter approximation for Gamma only rendering mode!
		// DVF = sqrt(DVF);
		// DVF = (a * sqrt(.25)) / (max(sqrt(0.1), lh)*sqrt(roughness + .5) * d);
		half specularTerm = a / (max(0.32h, lh) * (1.5h + roughness) * d);
	#else
		half specularTerm = a2 / (max(0.1h, lh*lh) * (roughness + 0.5h) * (d * d) * 4);
	#endif
    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
	#if defined (SHADER_API_MOBILE)
		specularTerm = specularTerm - 1e-4h;
	#endif
	#else
    // Legacy
    half specularPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
    // Modified with approximate Visibility function that takes roughness into account
    // Original ((n+1)*N.H^n) / (8*Pi * L.H^3) didn't take into account roughness
    // and produced extremely bright specular at grazing angles
    half invV = lh * lh * smoothness + perceptualRoughness * perceptualRoughness; // approx ModifiedKelemenVisibilityTerm(lh, perceptualRoughness);
    half invF = lh;
    half specularTerm = ((specularPower + 1) * pow (nh, specularPower)) / (8 * invV * invF + 1e-4h);
	#ifdef UNITY_COLORSPACE_GAMMA
		specularTerm = sqrt(max(1e-4h, specularTerm));
	#endif
	#endif
	#if defined (SHADER_API_MOBILE)
		specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
	#endif
	#if defined(_SPECULARHIGHLIGHTS_OFF)
		specularTerm = 0.0;
	#endif
    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(realRoughness^2+1)
    // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
    // 1-x^3*(0.6-0.08*x)   approximation for 1/(x^4+1)
	#ifdef UNITY_COLORSPACE_GAMMA
		half surfaceReduction = 0.28;
	#else
		half surfaceReduction = (0.6-0.08*perceptualRoughness);
	#endif
    surfaceReduction = 1.0 - roughness*perceptualRoughness*surfaceReduction;
    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));
    half3 term0 = CelStep(diffColor + (specColor * specularTerm));
	half3 term1 = CelStep(gi.diffuse * diffColor);
	half3 term2 = CelStep(surfaceReduction * gi.specular * FresnelLerpFast (specColor, grazingTerm, nv));
    //return term0;
    return (term0 * light.color * nl) + term1 + term2;	
}

half4 BRDF2_CelNoir_Raw (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi)
{
	half3 color = BDRF2_CelNoir_Internal(diffColor, specColor, oneMinusReflectivity, smoothness, normal, viewDir, light, gi);
    return half4(color, 1);
}

half4 BRDF2_CelNoir_Crushed (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi)
{
	half3 color = BDRF2_CelNoir_Internal(diffColor, specColor, oneMinusReflectivity, smoothness, normal, viewDir, light, gi);
    return half4(CelCrush(color), 1);
}

half3 BRDF3_Direct_CelNoir(half3 diffColor, half3 specColor, half rlPow4, half smoothness)
{
    half LUT_RANGE = 16.0; // must match range in NHxRoughness() function in GeneratedTextures.cpp
    // Lookup texture to save instructions
    half specular = tex2D(unity_NHxRoughness, half2(rlPow4, SmoothnessToPerceptualRoughness(smoothness))).UNITY_ATTEN_CHANNEL * LUT_RANGE;
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specular = 0.0;
#endif

    return CelCrush(CelStep(diffColor + specular * specColor));
}

half3 BRDF3_Indirect_CelNoir(half3 diffColor, half3 specColor, UnityIndirect indirect, half grazingTerm, half fresnelTerm)
{
    half3 c = CelStep(indirect.diffuse * diffColor);
    c += CelStep(indirect.specular * lerp (specColor, grazingTerm, fresnelTerm));
    return CelCrush(c);
}