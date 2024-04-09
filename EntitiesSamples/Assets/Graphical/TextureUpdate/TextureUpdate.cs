using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace Graphical.TextureUpdate
{
    public class TextureUpdate : MonoBehaviour
    {
        public enum UpdateType
        {
            MainThread,
            MainThreadBurst,
            ParallelBurstJob,
            VectorizedParallelBurstJob,
        }

        static ProfilerMarker s_MainThreadProfilerMarker = new ProfilerMarker("TextureUpdate." + UpdateType.MainThread);
        static ProfilerMarker s_MainThreadBurstProfilerMarker = new ProfilerMarker("TextureUpdate." + UpdateType.MainThreadBurst);
        static ProfilerMarker s_ParallelJobProfilerMarker = new ProfilerMarker("TextureUpdate." + UpdateType.ParallelBurstJob);
        static ProfilerMarker s_VectorizedParallelJobProfilerMarker = new ProfilerMarker("TextureUpdate." + UpdateType.VectorizedParallelBurstJob);

        public int2 textureSize = new int2(256, 256);
        public UpdateType updateType = UpdateType.MainThread;
        public Color color = Color.yellow;
        public float period = math.PI * 2;

        private Texture2D m_Texture;

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                if (m_Texture != null)
                {
                    textureSize.x = m_Texture.width;
                    textureSize.y = m_Texture.height;
                }
            }
            else
            {
                if (textureSize.x < 4) textureSize.x = 4;
                if (textureSize.y < 4) textureSize.y = 4;
                textureSize.x = (textureSize.x + 3) & (int.MaxValue - 3);
            }
        }

        void Start()
        {
            m_Texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);

            var renderer = GetComponent<Renderer>();
            renderer.material = Instantiate(renderer.material);
            renderer.material.mainTexture = m_Texture;
        }

        void Update()
        {
            var raw = m_Texture.GetRawTextureData<Color32>();
            var phase = Time.time;

            switch (updateType)
            {
                case UpdateType.MainThread:
                {
                    using (s_MainThreadProfilerMarker.Auto())
                    {
                        for (int i = 0; i < raw.Length; i += 1)
                        {
                            var x = i % textureSize.x + .5f;
                            var y = i / textureSize.x + .5f;
                            var normalized = (new float2(x, y) / textureSize) * 2 - new float2(1, 1);
                            var value = (math.sin(math.length(normalized) * period - phase) + 1) / 2;
                            raw[i] = color * value;
                        }
                    }

                    break;
                }

                case UpdateType.MainThreadBurst:
                {
                    using (s_MainThreadBurstProfilerMarker.Auto())
                    {
                        new BurstJob
                        {
                            raw = raw,
                            phase = phase,
                            size = textureSize,
                            color = color,
                            period = period
                        }.Run();
                    }

                    break;
                }

                case UpdateType.ParallelBurstJob:
                {
                    using (s_ParallelJobProfilerMarker.Auto())
                    {
                        new ParallelJob
                        {
                            raw = raw,
                            phase = phase,
                            size = textureSize,
                            color = color,
                            period = period
                        }.Schedule(textureSize.y, 1).Complete();
                    }

                    break;
                }

                case UpdateType.VectorizedParallelBurstJob:
                {
                    using (s_VectorizedParallelJobProfilerMarker.Auto())
                    {
                        new VectorizedParallelJob
                        {
                            raw = raw.Reinterpret<uint4>(4),
                            phase = phase,
                            size = textureSize,
                            color = (Vector4)color * 255,
                            period = period
                        }.Schedule(textureSize.y, 1).Complete();
                    }

                    break;
                }
            }

            m_Texture.Apply();
        }

        [BurstCompile]
        struct BurstJob : IJob
        {
            public NativeArray<Color32> raw;
            public float phase;
            public int2 size;
            public Color color;
            public float period;

            public void Execute()
            {
                for (int y = 0; y < size.y; y += 1)
                {
                    for (int x = 0; x < size.x; x += 1)
                    {
                        var normalized = (new float2(x + .5f, y + .5f) / size) * 2 - new float2(1, 1);
                        var value = (math.sin(math.length(normalized) * period - phase) + 1) / 2;
                        raw[x + y * size.x] = color * value;
                    }
                }
            }
        }

        [BurstCompile]
        struct ParallelJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<Color32> raw;
            public float phase;
            public int2 size;
            public Color color;
            public float period;

            public void Execute(int y)
            {
                for (int x = 0; x < size.x; x += 1)
                {
                    var normalized = (new float2(x + .5f, y + .5f) / size) * 2 - new float2(1, 1);
                    var value = (math.sin(math.length(normalized) * period - phase) + 1) / 2;
                    raw[x + y * size.x] = color * value;
                }
            }
        }

        [BurstCompile]
        struct VectorizedParallelJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<uint4> raw;
            public float phase;
            public int2 size;
            public float4 color;
            public float period;

            public void Execute(int y)
            {
                var y4 = new float4(y + .5f, y + .5f, y + .5f, y + .5f) / size.y * 2 - 1;
                var dx = (float4)(8f / size.x);
                var x4 = new float4(0.5f, 1.5f, 2.5f, 3.5f) / size.x * 2 - 1;

                for (int i = y * size.x; i < (y + 1) * size.x; i += 4)
                {
                    var distance4 = math.sqrt(x4 * x4 + y4 * y4);
                    var intensity4 = (math.sin(distance4 * period - phase) + 1) / 2;

                    uint4 rgba4 = (uint4)(intensity4 * color.x);
                    rgba4 += (uint4)(intensity4 * color.y) << 8;
                    rgba4 += (uint4)(intensity4 * color.z) << 16;
                    rgba4 += (uint4)(intensity4 * color.w) << 24;
                    raw[i / 4] = rgba4;

                    x4 += dx;
                }
            }
        }
    }
}
