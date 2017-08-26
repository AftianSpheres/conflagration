/*
    CelNoir standard shader core, forward rendering path

    Part of CelNoir cel shader package.
    
    Copyright (C) 2017 Tritonic Games.
    Do not redistribute or use without permission.
    
    Based on Unity built-in shader source, copyright (c) 2016 Unity Technologies,
    used in compliance with license. (see license_unity_builtins.txt)
*/

#include "UnityStandardConfig.cginc"

#if UNITY_STANDARD_SIMPLE
    #include "CelNoirStandardCoreForwardSimple.hlsl"
    VertexOutputBaseSimple vertBase (VertexInput v) { return vertForwardBaseSimple(v); }
    VertexOutputForwardAddSimple vertAdd (VertexInput v) { return vertForwardAddSimple(v); }
    half4 fragBase (VertexOutputBaseSimple i) : SV_Target { return fragForwardBaseSimpleInternal_CelNoir(i); }
    half4 fragAdd (VertexOutputForwardAddSimple i) : SV_Target { return fragForwardAddSimpleInternal_CelNoir(i); }
#else
    #include "CelNoirStandardCore.hlsl"
    VertexOutputForwardBase vertBase (VertexInput v) { return vertForwardBase(v); }
    VertexOutputForwardAdd vertAdd (VertexInput v) { return vertForwardAdd(v); }
    half4 fragBase (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseInternal_CelNoir(i); }
    half4 fragAdd (VertexOutputForwardAdd i) : SV_Target { return fragForwardAddInternal_CelNoir(i); }
#endif