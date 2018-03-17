using UnityEngine.Rendering;

namespace UnityEngine.PostProcessing
{
    public sealed class TaaComponent : PostProcessingComponentCommandBuffer<AntialiasingModel>
    {
        static class Uniforms
        {
            internal static int _Jitter               = Shader.PropertyToID("_Jitter");
            internal static int _SharpenParameters    = Shader.PropertyToID("_SharpenParameters");
            internal static int _FinalBlendParameters = Shader.PropertyToID("_FinalBlendParameters");
            internal static int _MainTex              = Shader.PropertyToID("_MainTex");
            internal static int _HistoryTex           = Shader.PropertyToID("_HistoryTex");
            internal static int _BlitSourceTex        = Shader.PropertyToID("_BlitSourceTex");
        }

        const string k_ShaderString = "Hidden/Post FX/Temporal Anti-aliasing";

        RenderTexture m_History;
        RenderTargetIdentifier m_HistoryIdentifier;
        readonly RenderTargetIdentifier[] m_MRT = new RenderTargetIdentifier[2];

        int m_SampleIndex;
        bool m_ResetHistory = true;

        public override bool active
        {
            get
            {
                return model.enabled
                       && model.settings.method == AntialiasingModel.Method.Taa
                       && SystemInfo.supportsMotionVectors;
            }
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        public override string GetName()
        {
            return "Temporal Anti-Aliasing";
        }

        public override CameraEvent GetCameraEvent()
        {
            return model.settings.taaSettings.renderQueue == AntialiasingModel.TaaQueue.BeforeTransparent
                   ? CameraEvent.BeforeImageEffectsOpaque
                   : CameraEvent.BeforeImageEffects;
        }

        public override void OnEnable()
        {
            ResetHistory();
        }

        public void ResetHistory()
        {
            m_ResetHistory = true;
        }

        public void SetProjectionMatrix()
        {
            var settings = model.settings.taaSettings;

            var jitter = GenerateRandomOffset();
            jitter *= settings.jitterSpread;

            context.camera.nonJitteredProjectionMatrix = context.camera.projectionMatrix;
            context.camera.projectionMatrix = context.camera.orthographic
                ? GetOrthographicProjectionMatrix(jitter)
                : GetPerspectiveProjectionMatrix(jitter);

#if UNITY_5_5_OR_NEWER
            context.camera.useJitteredProjectionMatrixForTransparentRendering =
                settings.renderQueue != AntialiasingModel.TaaQueue.BeforeTransparent;
#endif

            jitter.x /= context.width;
            jitter.y /= context.height;

            var material = context.materialFactory.Get(k_ShaderString);
            material.SetVector(Uniforms._Jitter, jitter);
        }

        public override void PopulateCommandBuffer(CommandBuffer cb)
        {
            var format = context.isHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            var material = context.materialFactory.Get(k_ShaderString);
            var settings = model.settings.taaSettings;

            if (m_History == null || m_History.width != context.width || m_History.height != context.height || !m_History.IsCreated())
            {
                OnDisable();

                m_History = RenderTexture.GetTemporary(context.width, context.height, 0, format, RenderTextureReadWrite.Default);
                m_History.filterMode = FilterMode.Bilinear;
                m_History.hideFlags = HideFlags.HideAndDontSave;

                m_HistoryIdentifier = new RenderTargetIdentifier(m_History);
                ResetHistory();
            }

            const float kMotionAmplification = 100f * 60f;
            material.SetVector(Uniforms._SharpenParameters, new Vector4(settings.sharpen, 0f, 0f, 0f));
            material.SetVector(Uniforms._FinalBlendParameters, new Vector4(settings.stationaryBlending, settings.motionBlending, kMotionAmplification, 0f));

            if (m_ResetHistory)
            {
                cb.SetGlobalTexture(Uniforms._MainTex, BuiltinRenderTextureType.CameraTarget);
                cb.Blit(BuiltinRenderTextureType.CameraTarget, m_HistoryIdentifier, material, 3);
                m_ResetHistory = false;
            }

            int tempTexture = Uniforms._BlitSourceTex;
            cb.GetTemporaryRT(tempTexture, context.width, context.height, 0, FilterMode.Bilinear, format);

            cb.SetGlobalTexture(Uniforms._HistoryTex, m_HistoryIdentifier);
            cb.SetGlobalTexture(Uniforms._MainTex, BuiltinRenderTextureType.CameraTarget);
            cb.Blit(BuiltinRenderTextureType.CameraTarget, tempTexture, material, context.camera.orthographic ? 1 : 0);

            m_MRT[0] = BuiltinRenderTextureType.CameraTarget;
            m_MRT[1] = m_HistoryIdentifier;

            cb.SetRenderTarget(m_MRT, BuiltinRenderTextureType.CameraTarget);
            cb.DrawMesh(GraphicsUtils.quad, Matrix4x4.identity, material, 0, 2);

            cb.ReleaseTemporaryRT(tempTexture);
        }

        float GetHaltonValue(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / (float)radix;

            while (index > 0)
            {
                result += (float)(index % radix) * fraction;

                index /= radix;
                fraction /= (float)radix;
            }

            return result;
        }

        Vector2 GenerateRandomOffset()
        {
            var offset = new Vector2(
                    GetHaltonValue(m_SampleIndex & 1023, 2),
                    GetHaltonValue(m_SampleIndex & 1023, 3));

            if (++m_SampleIndex >= model.settings.taaSettings.sampleCount)
                m_SampleIndex = 0;

            return offset;
        }

        // Adapted heavily from PlayDead's TAA code
        // https://github.com/playdeadgames/temporal/blob/master/Assets/Scripts/Extensions.cs
        Matrix4x4 GetPerspectiveProjectionMatrix(Vector2 offset)
        {
            float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * context.camera.fieldOfView);
            float horizontal = vertical * context.camera.aspect;

            offset.x *= horizontal / (0.5f * context.width);
            offset.y *= vertical / (0.5f * context.height);

            float left = (offset.x - horizontal) * context.camera.nearClipPlane;
            float right = (offset.x + horizontal) * context.camera.nearClipPlane;
            float top = (offset.y + vertical) * context.camera.nearClipPlane;
            float bottom = (offset.y - vertical) * context.camera.nearClipPlane;

            var matrix = new Matrix4x4();

            matrix[0, 0] = (2f * context.camera.nearClipPlane) / (right - left);
            matrix[0, 1] = 0f;
            matrix[0, 2] = (right + left) / (right - left);
            matrix[0, 3] = 0f;

            matrix[1, 0] = 0f;
            matrix[1, 1] = (2f * context.camera.nearClipPlane) / (top - bottom);
            matrix[1, 2] = (top + bottom) / (top - bottom);
            matrix[1, 3] = 0f;

            matrix[2, 0] = 0f;
            matrix[2, 1] = 0f;
            matrix[2, 2] = -(context.camera.farClipPlane + context.camera.nearClipPlane) / (context.camera.farClipPlane - context.camera.nearClipPlane);
            matrix[2, 3] = -(2f * context.camera.farClipPlane * context.camera.nearClipPlane) / (context.camera.farClipPlane - context.camera.nearClipPlane);

            matrix[3, 0] = 0f;
            matrix[3, 1] = 0f;
            matrix[3, 2] = -1f;
            matrix[3, 3] = 0f;

            return matrix;
        }

        Matrix4x4 GetOrthographicProjectionMatrix(Vector2 offset)
        {
            float vertical = context.camera.orthographicSize;
            float horizontal = vertical * context.camera.aspect;

            offset.x *= horizontal / (0.5f * context.width);
            offset.y *= vertical / (0.5f * context.height);

            float left = offset.x - horizontal;
            float right = offset.x + horizontal;
            float top = offset.y + vertical;
            float bottom = offset.y - vertical;

            return Matrix4x4.Ortho(left, right, bottom, top, context.camera.nearClipPlane, context.camera.farClipPlane);
        }

        public override void OnDisable()
        {
            if (m_History != null)
                RenderTexture.ReleaseTemporary(m_History);

            m_History = null;
            m_HistoryIdentifier = 0;
            m_SampleIndex = 0;
        }
    }
}
