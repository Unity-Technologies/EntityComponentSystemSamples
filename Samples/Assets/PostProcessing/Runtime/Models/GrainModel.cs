using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class GrainModel : PostProcessingModel
    {
        public enum Mode
        {
            Fast,
            Filmic
        }

        [Serializable]
        public struct Settings
        {
            [Tooltip("Grain mode. \"Filmic\" produces a high quality, camera-like grain. \"Fast\" is aimed at lower-end platforms as it's a lot faster but doesn't look as good as \"Filmic\".")]
            public Mode mode;

            [Range(0f, 1f), Tooltip("Grain strength. Higher means more visible grain.")]
            public float intensity;

            [Range(1.5f, 3f), Tooltip("Grain particle size in \"Filmic\" mode.")]
            public float size;

            [Range(0f, 1f), Tooltip("Controls the noisiness response curve based on scene luminance. Lower values mean less noise in dark areas.")]
            public float luminanceContribution;

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        mode = Mode.Filmic,
                        intensity = 0.12f,
                        size = 1.6f,
                        luminanceContribution = 0.75f
                    };
                }
            }
        }

        [SerializeField]
        Settings m_Settings = Settings.defaultSettings;
        public Settings settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }

        public override void Reset()
        {
            m_Settings = Settings.defaultSettings;
        }
    }
}
