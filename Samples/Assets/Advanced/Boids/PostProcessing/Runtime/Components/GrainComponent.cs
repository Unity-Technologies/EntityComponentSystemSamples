namespace UnityEngine.PostProcessing
{
    public sealed class GrainComponent : PostProcessingComponentRenderTexture<GrainModel>
    {
        static class Uniforms
        {
            internal static readonly int _Grain_Params1 = Shader.PropertyToID("_Grain_Params1");
            internal static readonly int _Grain_Params2 = Shader.PropertyToID("_Grain_Params2");
        }

        public override bool active
        {
            get
            {
                return model.enabled
                       && model.settings.intensity > 0f;
            }
        }

        public override void Prepare(Material uberMaterial)
        {
            var settings = model.settings;

            uberMaterial.EnableKeyword(
                settings.mode == GrainModel.Mode.Fast
                ? "GRAIN_FAST"
                : "GRAIN_FILMIC"
                );

#if POSTFX_DEBUG_STATIC_GRAIN
            float time = 4f;
#else
            float time = Time.realtimeSinceStartup;
#endif

            // Used for sample rotation in Filmic mode and position offset in Fast mode
            const float kRotationOffset = 1.425f;
            float c = Mathf.Cos(time + kRotationOffset);
            float s = Mathf.Sin(time + kRotationOffset);

            uberMaterial.SetVector(Uniforms._Grain_Params1, new Vector4(settings.intensity * 0.25f, settings.size, settings.luminanceContribution, (float)context.width / (float)context.height));
            uberMaterial.SetVector(Uniforms._Grain_Params2, new Vector3(c, s, time / 20f));
        }
    }
}
