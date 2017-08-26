/*
    CelNoir screen-space shadow shader

    Part of CelNoir cel shader package.
    
    Copyright (C) 2017 Tritonic Games.
    Do not redistribute or use without permission.
    
    Based on Unity built-in shader source, copyright (c) 2016 Unity Technologies,
    used in compliance with license. (see license_unity_builtins.txt)
*/

// Collects cascaded shadows into screen space buffer
Shader "CelNoir/Hidden/ScreenSpaceShadows" {

Properties {
    _ShadowMapTexture ("", any) = "" {}
}

// ----------------------------------------------------------------------------------------
// Subshader for hard shadows:
// Just collect shadows into the buffer. Used on pre-SM3 GPUs and when hard shadows are picked.

SubShader {
    Tags{ "ShadowmapFilter" = "HardShadow" }
    Pass {
        ZWrite Off ZTest Always Cull Off

        HLSLPROGRAM
		#include "CelNoirScreenSpaceShadowsCore.hlsl"

        #pragma vertex vert
        #pragma fragment frag_hard
        #pragma multi_compile_shadowcollector

        inline float3 computeCameraSpacePosFromDepth(v2f i)
        {
            return computeCameraSpacePosFromDepthAndVSInfo(i);
        }
        ENDHLSL
    }
}

// ----------------------------------------------------------------------------------------
// Subshader for hard shadows:
// Just collect shadows into the buffer. Used on pre-SM3 GPUs and when hard shadows are picked.
// This version does inv projection at the PS level, slower and less precise however more general.

SubShader {
    Tags{ "ShadowmapFilter" = "HardShadow_FORCE_INV_PROJECTION_IN_PS" }
    Pass{
        ZWrite Off ZTest Always Cull Off

        HLSLPROGRAM
		#include "CelNoirScreenSpaceShadowsCore.hlsl"

        #pragma vertex vert
        #pragma fragment frag_hard
        #pragma multi_compile_shadowcollector

        inline float3 computeCameraSpacePosFromDepth(v2f i)
        {
            return computeCameraSpacePosFromDepthAndInvProjMat(i);
        }
        ENDHLSL
    }
}

// ----------------------------------------------------------------------------------------
// Subshader that does soft PCF filtering while collecting shadows.
// Requires SM3 GPU.

Subshader {
    Tags {"ShadowmapFilter" = "PCF_SOFT"}
    Pass {
        ZWrite Off ZTest Always Cull Off

        HLSLPROGRAM
		#include "CelNoirScreenSpaceShadowsCore.hlsl"

        #pragma vertex vert
        #pragma fragment frag_pcfSoft
        #pragma multi_compile_shadowcollector
        #pragma target 3.0

        inline float3 computeCameraSpacePosFromDepth(v2f i)
        {
            return computeCameraSpacePosFromDepthAndVSInfo(i);
        }
        ENDHLSL
    }
}

// ----------------------------------------------------------------------------------------
// Subshader that does soft PCF filtering while collecting shadows.
// Requires SM3 GPU.
// This version does inv projection at the PS level, slower and less precise however more general.

Subshader{
    Tags{ "ShadowmapFilter" = "PCF_SOFT_FORCE_INV_PROJECTION_IN_PS" }
    Pass{
        ZWrite Off ZTest Always Cull Off

        HLSLPROGRAM
		#include "CelNoirScreenSpaceShadowsCore.hlsl"

        #pragma vertex vert
        #pragma fragment frag_pcfSoft
        #pragma multi_compile_shadowcollector
        #pragma target 3.0

        inline float3 computeCameraSpacePosFromDepth(v2f i)
        {
            return computeCameraSpacePosFromDepthAndInvProjMat(i);
        }
        ENDHLSL
    }
}

Fallback Off
}
