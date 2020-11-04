using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class TerrainDemoScene : SceneCreationSettings
{
    public int SizeX;
    public int SizeZ;
    public float ScaleX;
    public float ScaleY;
    public float ScaleZ;
    public TerrainCollider.CollisionMethod Method;
}

public class TerrainDemo : SceneCreationAuthoring<TerrainDemoScene>
{
    public int SizeX;
    public int SizeZ;
    public float ScaleX;
    public float ScaleY;
    public float ScaleZ;
    public TerrainCollider.CollisionMethod Method;

    public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TerrainDemoScene
        {
            DynamicMaterial = DynamicMaterial,
            StaticMaterial = StaticMaterial,
            Method = Method,
            SizeX = SizeX,
            SizeZ = SizeZ,
            ScaleX = ScaleX,
            ScaleY = ScaleY,
            ScaleZ = ScaleZ
        });
    }
}

public class TerrainDemoSystem : SceneCreationSystem<TerrainDemoScene>
{
    public override void CreateScene(TerrainDemoScene sceneSettings)
    {
        // Make heightfield data
        NativeArray<float> heights;
        int2 size;
        float3 scale;
        bool simple = false;
#if UNITY_ANDROID || UNITY_IOS
        simple = true;
#endif
        bool flat = false;
        bool mountain = false;
        if (simple)
        {
            size = new int2(2, 2);
            scale = new float3(25, 0.1f, 25);
            heights = new NativeArray<float>(size.x * size.y * UnsafeUtility.SizeOf<float>(), Allocator.Temp);
            heights[0] = 1;
            heights[1] = 0;
            heights[2] = 0;
            heights[3] = 1;
        }
        else
        {
            size = new int2(sceneSettings.SizeX, sceneSettings.SizeZ);
            scale = new float3(sceneSettings.ScaleX, sceneSettings.ScaleY, sceneSettings.ScaleZ);
            float period = 50.0f;
            heights = new NativeArray<float>(size.x * size.y * UnsafeUtility.SizeOf<float>(), Allocator.Temp);
            for (int j = 0; j < size.y; j++)
            {
                for (int i = 0; i < size.x; i++)
                {
                    float a = (i + j) * 2.0f * (float)math.PI / period;
                    heights[i + j * size.x] = flat ? 0.0f : math.sin(a);
                    if (mountain)
                    {
                        float fractionFromCenter = 1.0f - math.min(math.length(new float2(i - size.x / 2, j - size.y / 2)) / (math.min(size.x, size.y) / 2), 1.0f);
                        float mountainHeight = math.smoothstep(0.0f, 1, fractionFromCenter) * 25.0f;
                        heights[i + j * size.x] += mountainHeight;
                    }
                }
            }
        }

        // static terrain
        Entity staticEntity;
        {
            bool createMesh = false;
            var collider = createMesh
                ? CreateMeshTerrain(heights, new int2(sceneSettings.SizeX, sceneSettings.SizeZ), new float3(sceneSettings.ScaleX, sceneSettings.ScaleY, sceneSettings.ScaleZ))
                : TerrainCollider.Create(heights, size, scale, sceneSettings.Method);
            CreatedColliders.Add(collider);

            bool compound = false;
            if (compound)
            {
                var instances = new NativeArray<CompoundCollider.ColliderBlobInstance>(4, Allocator.Temp);
                for (int i = 0; i < 4; i++)
                {
                    instances[i] = new CompoundCollider.ColliderBlobInstance
                    {
                        Collider = collider,
                        CompoundFromChild = new RigidTransform
                        {
                            pos = new float3((i % 2) * scale.x * (size.x - 1), 0.0f, (i / 2) * scale.z * (size.y - 1)),
                            rot = quaternion.identity
                        }
                    };
                }
                collider = Unity.Physics.CompoundCollider.Create(instances);
                CreatedColliders.Add(collider);
                instances.Dispose();
            }

            float3 position = new float3(size.x - 1, 0.0f, size.y - 1) * scale * -0.5f;
            staticEntity = CreateStaticBody(position, quaternion.identity, collider);
        }
    }

    static BlobAssetReference<Collider> CreateMeshTerrain(NativeArray<float> heights, int2 size, float3 scale)
    {
        var vertices = new NativeList<float3>(Allocator.Temp);
        var triangles = new NativeList<int3>(Allocator.Temp);
        var vertexIndex = 0;
        for (int i = 0; i < size.x - 1; i++)
            for (int j = 0; j < size.y - 1; j++)
            {
                int i0 = i;
                int i1 = i + 1;
                int j0 = j;
                int j1 = j + 1;
                float3 v0 = new float3(i0, heights[i0 + size.x * j0], j0) * scale;
                float3 v1 = new float3(i1, heights[i1 + size.x * j0], j0) * scale;
                float3 v2 = new float3(i0, heights[i0 + size.x * j1], j1) * scale;
                float3 v3 = new float3(i1, heights[i1 + size.x * j1], j1) * scale;

                vertices.Add(v1);
                vertices.Add(v0);
                vertices.Add(v2);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                triangles.Add(new int3(vertexIndex++, vertexIndex++, vertexIndex++));
                triangles.Add(new int3(vertexIndex++, vertexIndex++, vertexIndex++));
            }

        return MeshCollider.Create(vertices, triangles);
    }
}
