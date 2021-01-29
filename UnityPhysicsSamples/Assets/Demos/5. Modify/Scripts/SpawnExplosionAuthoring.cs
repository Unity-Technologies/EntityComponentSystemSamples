using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;

struct SpawnExplosionSettings : ISpawnSettings, IComponentData
{
    #region ISpawnSettings
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }

    #endregion

    public int Id;
    public int Countdown;
    public float Force;
    public Entity Source;
}

public class SpawnExplosionAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [Header("Debris")]
    public GameObject Prefab;
    public int Count;

    [Header("Explosion")]
    public int Countdown;
    public float Force;

    private static int Id = -1;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SpawnExplosionSettings
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            Position = transform.position,
            Rotation = quaternion.identity,
            Count = Count,

            Id = Id--,
            Countdown = Countdown,
            Force = Force,
            Source = Entity.Null,
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) => referencedPrefabs.Add(Prefab);
}

class SpawnExplosionSystem : SpawnRandomObjectsSystemBase<SpawnExplosionSettings>
{
    // Used to divide colliders into groups, and to create a single collider for each group
    internal int GroupId;
    internal PhysicsCollider GroupCollider;
    internal NativeList<PhysicsCollider> CreatedColliders;

    protected override void OnCreate()
    {
        GroupId = 1;
        CreatedColliders = new NativeList<PhysicsCollider>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        foreach (var collider in CreatedColliders)
        {
            collider.Value.Dispose();
        }
        CreatedColliders.Dispose();
    }

    internal override void ConfigureInstance(Entity instance, ref SpawnExplosionSettings spawnSettings)
    {
        // Create single collider per Explosion group
        if (GroupId != spawnSettings.Id)
        {
            GroupId = spawnSettings.Id;
            spawnSettings.Source = instance;

            var collider = EntityManager.GetComponentData<PhysicsCollider>(instance);
            var memsize = collider.Value.Value.MemorySize;
            var oldFilter = collider.Value.Value.Filter;

            // Only one of these needed per group, since all debris within
            // a group will share a single collider
            // This will make debris within a group collide some time after the explosion happens
            EntityManager.AddComponentData(instance, new ChangeFilterCountdown
            {
                Countdown = spawnSettings.Countdown * 2,
                Filter = oldFilter
            });

            // Make unique collider for each spawned group
            unsafe
            {
                // Create new collider for the group
                // TODO : make Collider ICloneable
                BlobAssetReference<Collider> colliderCopy = BlobAssetReference<Collider>.Create(UnsafeUtility.Malloc(memsize, 16, Allocator.Persistent), memsize);
                UnsafeUtility.MemCpy(colliderCopy.GetUnsafePtr(), collider.ColliderPtr, memsize);

                // Set the GroupIndex to GroupId, which is negative
                // This ensures that the debris within a group don't collide
                colliderCopy.Value.Filter = new CollisionFilter
                {
                    BelongsTo = oldFilter.BelongsTo,
                    CollidesWith = oldFilter.CollidesWith,
                    GroupIndex = GroupId,
                };
                PhysicsCollider newCollider = new PhysicsCollider
                {
                    Value = colliderCopy
                };
                GroupCollider = newCollider;
                CreatedColliders.Add(GroupCollider);
            }
        }

        EntityManager.SetComponentData(instance, GroupCollider);

        EntityManager.AddComponentData(instance, new ExplosionCountdown
        {
            Source = spawnSettings.Source,
            Countdown = spawnSettings.Countdown,
            Center = spawnSettings.Position,
            Force = spawnSettings.Force
        });
    }
}
