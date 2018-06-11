using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Jobs.Tests
{
    [BurstCompile]
    public struct AliasTest1 : IJob
    {
        private int field1;
#pragma warning disable 0169 // "never used" warning
        private int pad0;
        private int pad1;
        private int pad2;
        private int pad3;
#pragma warning restore 0169
        private int field2;

        public void DoTheThing(ref int x)
        {
            x = x + 1;
        }

        public void Execute()
        {
            field1 = 17;
            field2 = field1 + 1;
            DoTheThing(ref field2);
            field1 = field2 + 1;
        }
    }

    [BurstCompile]
    public struct AliasTest2 : IJob
    {
        [ReadOnly]
        public NativeArray<int> a;

        public NativeArray<int> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                b[i] = a[i];
            }
        }
    }

    [BurstCompile]
    public struct AliasTest3 : IJob
    {
        [ReadOnly]
        public NativeArray<int> a;

        public NativeArray<int> b;

        public void Execute()
        {
            NativeArray<int> acopy = a;
            for (int i = 0; i < acopy.Length; ++i)
            {
                b[i] = acopy[i];
            }
        }
    }

    [BurstCompile]
    public struct AliasTest4 : IJob
    {
        public NativeArray<int> a;
        public int test1;
        public int test2;

        public void Execute()
        {
            test1 = 12;
            a[0] = 13;
            test2 = test1;
        }
    }

    [BurstCompile]
    public struct AliasTest5 : IJob
    {
        [ReadOnly]
        public NativeArray<int> a;

        public NativeArray<int> b;

        public void Execute()
        {
            #if true
            for (int i = 0; i < a.Length - 4; ++i)
            {
                int av = a[i];
                int bv = b[i];

                if (av > 100 || av < 50 || a[i + 4] > 10)
                {
                    bv = av * 123 + bv * 567 + av ^ 12;
                }

                b[i] = bv;
            }
    #else
            for (int i = 0; i < a.Length; ++i)
            {
                int av = a[i];

                if (av > 100)
                {
                    b[i] = av + 123;
                }
            }
            #endif
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low)]
    public struct AliasTest6 : IJob
    {
        public struct Sphere
        {
            public float x, y, z, r;

            public bool Intersects(Sphere other)
            {
                float dx = x - other.x;
                float dy = y - other.y;
                float dz = z - other.z;
                float rs = r + other.r;
                return dx * dx + dy * dy + dz * dz < (rs * rs);
            }
        }

        public Sphere test;

        [ReadOnly]
        public NativeArray<Sphere> a;

        public NativeArray<int> b;

        public void Execute()
        {
            int o = 0;
            for (int i = 0; i < a.Length; ++i)
            {
                Sphere s = a[i];
                int k = 0;
                if (s.Intersects(test))
                {
                    k = 1;
                }
                b[o] = k;
            }
        }
    }

    [BurstCompile]
    public struct AliasTest7 : IJob
    {
        [ReadOnly] public NativeArray<float> a;
        [ReadOnly] public NativeArray<float> b;
        public NativeArray<float> result;

        public void Execute()
        {
            for (int i = 0; i < 8; ++i)
            {
                result[i] = a[i] + b[i];
            }
        }
    }

    [BurstCompile]
    public struct PartialWrite : IJob
    {
        [ReadOnly]
        public NativeArray<int> a;

        public NativeArray<int> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                int av = a[i];

                if (av > 100)
                {
                    b[i] = av;
                }
            }
        }
    }

    [BurstCompile]
    public struct PartialWriteWorkaround : IJob
    {
        [ReadOnly]
        public NativeArray<int> a;

        public NativeArray<int> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                int av = a[i];

                int v = b[i];

                if (av > 100)
                {
                    v = av;
                }

                b[i] = v;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct IntToFloatPenalty : IJob
    {
        [ReadOnly]
        public NativeArray<float> b;

        public void Execute()
        {
            int k = 100;
            for (int i = 0; i < b.Length; ++i)
            {
                b[i] = k;
                ++k;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct IntToFloatPenaltyWorkaround : IJob
    {
        [ReadOnly]
        public NativeArray<float> b;

        public void Execute()
        {
            float k = 100;
            for (int i = 0; i < b.Length; ++i)
            {
                b[i] = k;
                k += 1.0f;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct IntToFloatPenaltyWorkaroundUnroll : IJob
    {
        [ReadOnly]
        public NativeArray<float> b;

        public void Execute()
        {
            float k0 = 100;
            float k1 = 101;
            float k2 = 102;
            float k3 = 103;
            for (int i = 0; i < b.Length; i += 4)
            {
                b[i + 0] = k0;
                b[i + 1] = k1;
                b[i + 2] = k2;
                b[i + 3] = k3;
                k0 += 1.0f;
                k1 += 1.0f;
                k2 += 1.0f;
                k3 += 1.0f;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct SquareRootRecip : IJob
    {
        [ReadOnly]
        public NativeArray<float> a;
        public NativeArray<float> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                b[i] = math.rsqrt(a[i]);
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct SquareRoot : IJob
    {
        [ReadOnly]
        public NativeArray<float> a;
        public NativeArray<float> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                b[i] = math.sqrt(a[i]);
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct DivisionByConstantPowerOfTwo : IJob
    {
        [ReadOnly]
        public NativeArray<float> a;
        public NativeArray<float> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                b[i] = a[i] / 128.0f;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct DivisionByConstant : IJob
    {
        [ReadOnly]
        public NativeArray<float> a;
        public NativeArray<float> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                b[i] = a[i] / 3.0f;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct DivisionByScalar : IJob
    {
        public float divisor;
        [ReadOnly]
        public NativeArray<float> a;
        public NativeArray<float> b;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                b[i] = a[i] / divisor;
            }
        }
    }

    [BurstCompile(Accuracy = Accuracy.Low, Support = Support.Relaxed)]
    public struct DivisionByVariable : IJob
    {
        [ReadOnly]
        public NativeArray<float> a;
        [ReadOnly]
        public NativeArray<float> b;

        public NativeArray<float> c;

        public void Execute()
        {
            for (int i = 0; i < a.Length; ++i)
            {
                c[i] = a[i] / b[i] * 3.0f;
            }
        }
    }
}


