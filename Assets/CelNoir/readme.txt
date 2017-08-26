A brief outline of what this is and how to use it:

CelNoir provides in-kind replacements for the metallic/smooth Unity standard shader and the hidden deferred lighting/deferred reflection/screen-space shadow shaders.

To use the standard shader replacement, just replace the standard shader on a material with CelNoir/Standard.

To use the other shaders, open Edit>Project Settings>Graphics Settings, go to the Built-in Shader Settings section, and replace
the following using the Custom Shader option in the dropdown:

Deferred: CelNoir/Hidden/DeferredLighting
Deferred Reflections: CelNoir/Hidden/DeferredReflections
Screen Space Shadows: CelNoir/Hidden/ScreenSpaceShadows

Configuration:

You can adjust the number of shading "steps" produced by the shaders and the luminance threshold for the crush-to-black effect by modifying the constants defined in CelNoir/Resources/Shader/CelNoirConfig.hlsl.

Information on rendering paths:

It is strongly recommended that you use the Deferred lighting path. The standard shader attempts to create the same aesthetic in both standard and forward rendering paths, but the nature of the operations CelNoir performs makes it difficult to guarantee that 100 percent. The Deferred path is the path that CelNoir is designed for, and if you're using the complex lighting setups that make these shaders look best you should generally be using the Deferred path anyway.

The intended usage case of the forward rendering support is for handling of partial-transparent objects and others that, for whatever reason, can't be rendered in the deferred pass.

CelNoir does not support the legacy vertex-lit rendering path in any form. The legacy deferred path is officially unsupported, but does function; however, as it is unsupported, you shouldn't use it unless you're comfortable extending and debugging the shaders on your own.