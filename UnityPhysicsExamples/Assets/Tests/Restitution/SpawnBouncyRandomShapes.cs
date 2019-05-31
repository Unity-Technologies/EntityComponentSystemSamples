using System;
using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Unity.Transforms;

[Serializable]
public class SpawnBouncyRandomShapes : MonoBehaviour
{
    public GameObject prefab;
    public float3 range;
    public int count;

    unsafe void OnEnable()
    {
        if (this.enabled)
        {
            // Create entity prefab from the game object hierarchy once
            Entity sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
            var entityManager = World.Active.EntityManager;

            var positions = new NativeArray<float3>(count, Allocator.Temp);
            var rotations = new NativeArray<quaternion>(count, Allocator.Temp);
            RandomPointsOnCircle(transform.position, range, ref positions, ref rotations);

            PhysicsCollider collider = entityManager.GetComponentData<PhysicsCollider>(sourceEntity);

            Unity.Physics.Material material = ((ConvexColliderHeader*)collider.ColliderPtr)->Material;
            material.Restitution = 1.0f;
            ((ConvexColliderHeader*)collider.ColliderPtr)->Material = material;

            for (int i = 0; i < count; i++)
            {
                var instance = entityManager.Instantiate(sourceEntity);
                entityManager.SetComponentData(instance, new Translation { Value = positions[i] });
                entityManager.SetComponentData(instance, new Rotation { Value = rotations[i] });
                entityManager.SetComponentData(instance, collider);
            }

            positions.Dispose();
            rotations.Dispose();
        }
    }

    public static void RandomPointsOnCircle(float3 center, float3 range, ref NativeArray<float3> positions, ref NativeArray<quaternion> rotations)
    {
        var count = positions.Length;
        // initialize the seed of the random number generator 
        Unity.Mathematics.Random random = new Unity.Mathematics.Random();
        random.InitState(10);
        for (int i = 0; i < count; i++)
        {
            positions[i] = center + random.NextFloat3(-range, range);
            rotations[i] = random.NextQuaternionRotation();
        }
    }
}
