using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using static Unity.Physics.Math;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics.Systems;
using Unity.Burst;
using Unity.Jobs;

public class TerrainDemo : BasePhysicsDemo
{
    public int SizeX;
    public int SizeZ;
    public float ScaleX;
    public float ScaleY;
    public float ScaleZ;
    public Unity.Physics.TerrainCollider.CollisionMethod Method;

    protected unsafe override void Start()
    {
        float3 gravity = new float3(0, -9.81f, 0);
        base.init(gravity);

        // Make heightfield data
        float* heights;
        int2 size;
        float3 scale;
        bool simple = false;
        bool flat = false;
        bool mountain = false;
        if (simple)
        {
            size = new int2(2, 2);
            scale = new float3(1, 0.1f, 1);
            heights = (float*)UnsafeUtility.Malloc(size.x * size.y * sizeof(float), 4, Allocator.Temp);
            heights[0] = 1;
            heights[1] = 0;
            heights[2] = 0;
            heights[3] = 1;
        }
        else
        {
            size = new int2(SizeX, SizeZ);
            scale = new float3(ScaleX, ScaleY, ScaleZ);
            float period = 50.0f;
            heights = (float*)UnsafeUtility.Malloc(size.x * size.y * sizeof(float), 4, Allocator.Temp);
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
            BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.TerrainCollider.Create(size, scale, heights, Method);

            bool convertToMesh = false;
            if (convertToMesh)
            {
#pragma warning disable 618
                var res = Unity.Physics.Authoring.DisplayBodyColliders.DrawComponent.BuildDebugDisplayMesh((Unity.Physics.Collider*)collider.GetUnsafePtr());
#pragma warning restore 618
                Vector3[] v = res[0].Mesh.vertices;
                float3[] vertices = new float3[v.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = v[i];
                }
                collider = Unity.Physics.MeshCollider.Create(vertices, res[0].Mesh.triangles);
            }

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
                instances.Dispose();
            }

            float3 position = new float3(size.x - 1, 0.0f, size.y - 1) * scale * -0.5f;
            staticEntity = CreateStaticBody(position, quaternion.identity, collider);
        }

        UnsafeUtility.Free(heights, Allocator.Temp);
    }
}
