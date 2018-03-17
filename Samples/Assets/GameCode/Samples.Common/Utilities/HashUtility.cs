using Unity.Mathematics;

namespace Samples.Common
{
    public struct GridHash
    {
        public readonly static int3[] cellOffsets =
        {
            new int3(0, 0, 0),
            new int3(-1, 0, 0),
            new int3(0, -1, 0),
            new int3(0, 0, -1),
            new int3(1, 0, 0),
            new int3(0, 1, 0),
            new int3(0, 0, 1)
        };
    
        public readonly static int2[] cell2DOffsets =
        {
            new int2(0, 0),
            new int2(-1, 0),
            new int2(0, -1),
            new int2(1, 0),
            new int2(0, 1),
        };

        public static int Hash(float3 v, float cellSize)
        {
            return Hash(Quantize(v, cellSize));
        }

        public static int3 Quantize(float3 v, float cellSize)
        {
            return new int3(math.floor(v / cellSize));
        }
    
        public static int Hash(float2 v, float cellSize)
        {
            return Hash(Quantize(v, cellSize));
        }

        public static int2 Quantize(float2 v, float cellSize)
        {
            return new int2(math.floor(v / cellSize));
        }

        public static int Hash(int3 grid)
        {
            unchecked
            {
                // Simple int3 hash based on a pseudo mix of :
                // 1) https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
                // 2) https://en.wikipedia.org/wiki/Jenkins_hash_function
                int hash = grid.x;
                hash = (hash * 397) ^ grid.y;
                hash = (hash * 397) ^ grid.z;
                hash += hash << 3;
                hash ^= hash >> 11;
                hash += hash << 15;
                return hash;
            }
        }
    
        public static int Hash(int2 grid)
        {
            unchecked
            {
                // Simple int3 hash based on a pseudo mix of :
                // 1) https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
                // 2) https://en.wikipedia.org/wiki/Jenkins_hash_function
                int hash = grid.x;
                hash = (hash * 397) ^ grid.y;
                hash += hash << 3;
                hash ^= hash >> 11;
                hash += hash << 15;
                return hash;
            }
        }

        public static ulong Hash(ulong hash, ulong key)
        {
            const ulong m = 0xc6a4a7935bd1e995UL;
            const int r = 47;

            ulong h = hash;
            ulong k = key;

            k *= m;
            k ^= k >> r;
            k *= m;

            h ^= k;
            h *= m;

            h ^= h >> r;
            h *= m;
            h ^= h >> r;

            return h;
        }
    }
}
