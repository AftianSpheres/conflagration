using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace PostProcessingExtensions
{
    public enum EdgeDetectMode
    {
        TriangleDepthNormals = 0,
        RobertsCrossDepthNormals = 1,
        SobelDepth = 2,
        SobelDepthThin = 3,
        TriangleLuminance = 4
    }

    [Serializable]
    public sealed class EdgeDetectModeParameter : ParameterOverride<EdgeDetectMode> { }

    [Serializable]
    [PostProcess(typeof(EdgeDetectRenderer), PostProcessEvent.BeforeStack, "Extensions/Edge Detect", true)]
    public sealed class EdgeDetect : PostProcessEffectSettings
    {
        [Tooltip("The edge detection algorithm to use.")]
        public EdgeDetectModeParameter mode = new EdgeDetectModeParameter { value = EdgeDetectMode.TriangleDepthNormals };

        [DisplayName("Sensitivity (Depth)"), Min(0f), Tooltip("Sensitivity to differences in depth. Higher values cause edges to be detected on smaller differences.")]
        public FloatParameter sensitivityDepth = new FloatParameter { value = 1.0f };

        [DisplayName("Sensitivity (Normals)"), Min(0f), Tooltip("Sensitivity to differences in normals. Higher values cause edges to be detected on smaller differences.")]
        public FloatParameter sensitivityNormals = new FloatParameter { value = 1.0f };

        [DisplayName("Luminance Threshold"), Min(0f), Tooltip("Threshold to use for detecting luminance changes. Lower values cause edges to be detected on smaller differences.")]
        public FloatParameter lumThreshold = new FloatParameter { value = 0.2f };

        [DisplayName("Edge Exponent"), Min(0f), Tooltip("Exponent to use in Sobel operators. Lower values cause edges to be detected on smaller differences..")]
        public FloatParameter edgeExp = new FloatParameter { value = 1.0f };

        [DisplayName("Sampling Distance"), Min(0f), Tooltip("Larger sampling distances create thicker edges, but may introduce artifacts.")]
        public FloatParameter sampleDist = new FloatParameter { value = 1.0f };

        [Range(0f, 1f), Tooltip("If greater than zero, blend with fixed background color.")]
        public FloatParameter edgesOnly = new FloatParameter { value = 0.0f };

        [DisplayName("BG Color"), ColorUsage(true, false, 0, 8, 1/8, 3), Tooltip("Background color to use if edges-only property is nonzero.")]
        public ColorParameter edgesOnlyBgColor = new ColorParameter { value = Color.white };
    }

    public sealed class EdgeDetectRenderer : PostProcessEffectRenderer<EdgeDetect>
    {
        private readonly static Shader shader = Shader.Find("Hidden/Extensions/EdgeDetect");

        public override DepthTextureMode GetCameraFlags()
        {          
            if (settings.mode == EdgeDetectMode.SobelDepth || settings.mode == EdgeDetectMode.SobelDepthThin) return DepthTextureMode.Depth;
            else if (settings.mode == EdgeDetectMode.TriangleDepthNormals || settings.mode == EdgeDetectMode.RobertsCrossDepthNormals) return DepthTextureMode.DepthNormals;
            else return DepthTextureMode.None;
        }

        public override void Render (PostProcessRenderContext context)
        {
            Vector2 sensitivity = new Vector2(settings.sensitivityDepth, settings.sensitivityNormals);
            PropertySheet sheet = context.propertySheets.Get(shader);
            sheet.properties.Clear();     
            sheet.properties.SetVector("_Sensitivity", new Vector4(sensitivity.x, sensitivity.y, 1.0f, sensitivity.y));
            sheet.properties.SetFloat("_BgFade", settings.edgesOnly);
            sheet.properties.SetFloat("_SampleDistance", settings.sampleDist);
            sheet.properties.SetVector("_BgColor", new Vector4(0, 0, 0, 0));
            sheet.properties.SetFloat("_Exponent", settings.edgeExp);
            sheet.properties.SetFloat("_Threshold", settings.lumThreshold);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)settings.mode.value);
        }
    }
}
