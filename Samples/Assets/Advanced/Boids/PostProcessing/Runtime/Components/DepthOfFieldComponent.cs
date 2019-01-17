using UnityEngine.Rendering;

namespace UnityEngine.PostProcessing
{
    // References :
    //  This DOF implementation use ideas from public sources, a big thank to them :
    //  http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
    //  http://www.crytek.com/download/Sousa_Graphics_Gems_CryENGINE3.pdf
    //  http://graphics.cs.williams.edu/papers/MedianShaderX6/
    //  http://http.developer.nvidia.com/GPUGems/gpugems_ch24.html
    //  http://vec3.ca/bicubic-filtering-in-fewer-taps/

    public sealed class DepthOfFieldComponent : PostProcessingComponentCommandBuffer<DepthOfFieldModel>
    {
        const string k_ShaderString = "Hidden/Post FX/Depth Of Field";

        public override bool active
        {
            get { return model.enabled; }
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        public override string GetName()
        {
            return "Depth of Field";
        }

        public override CameraEvent GetCameraEvent()
        {
            return CameraEvent.BeforeImageEffectsOpaque;
        }

        public override void PopulateCommandBuffer(CommandBuffer cb)
        {
        }
    }
}
