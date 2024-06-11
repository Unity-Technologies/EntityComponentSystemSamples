// This code is used in the 5g2. Unique Collider Blob Sharing demo and inherits from SpawnRandomObjectsSystemBase
// The OnUpdate method in SpawnRandomObjectsSystemBase will spawn an explosion group where this is defined in
// ConfigureInstance().
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
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
    public int RandomSeedOffset { get; set; }
    #endregion

    public int Id;
    public int Countdown;
    public float Force;
    public Entity Source;
}

public class SpawnExplosionAuthoring : MonoBehaviour
{
    [Header("Debris")]
    public GameObject Prefab;
    public int Count;

    [Header("Explosion")]
    public int Countdown;
    public float Force;

    internal static int Id = -1;
}

class SpawnExplosionAuthoringBaker : Baker<SpawnExplosionAuthoring>
{
    public override void Bake(SpawnExplosionAuthoring authoring)
    {
        var transform = GetComponent<Transform>();
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new SpawnExplosionSettings
        {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            Position = transform.position,
            Rotation = quaternion.identity,
            Count = authoring.Count,

            Id = SpawnExplosionAuthoring.Id--,
            Countdown = authoring.Countdown,
            Force = authoring.Force,
            Source = Entity.Null,
        });
    }
}

// The data set in ConfigureInstance feeds into the OnUpdate method of SpawnRandomObjectsSystemBase. The OnUpdate will
// loop through the number of explosion group instances (rockets) and this system specifies the prefab to instantiate
// for the fireworks pieces is the ExplosionDebris prefab.
partial class SpawnExplosionSystem : SpawnRandomObjectsSystemBase<SpawnExplosionSettings>
{
    // Used to divide colliders into groups, and to create a single collider for each group
    internal int GroupId;
    internal PhysicsCollider GroupCollider;

    protected override void OnCreate()
    {
        GroupId = 1;
    }

    protected override void OnDestroy() {}

    /// <summary>
    /// When this method is called for the first time, the collider of the ExplosionDebris instance is made unique.
    /// On subsequent calls, the GroupId will already match the spawnSettings.Id and the collider data will be
    /// updated to the collider data set in the first call. Therefore, the debris of each explosion group (rocket)
    /// is shared within the same rocket, but is unique for each rocket instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="spawnSettings"></param>
    internal override void ConfigureInstance(Entity instance, ref SpawnExplosionSettings spawnSettings)
    {
        // Create single collider per Explosion group
        if (GroupId != spawnSettings.Id)
        {
            GroupId = spawnSettings.Id;
            spawnSettings.Source = instance;

            var collider = EntityManager.GetComponentData<PhysicsCollider>(instance);
            var oldFilter = collider.Value.Value.GetCollisionFilter();

            // Only one of these needed per group, since all debris within
            // a group will share a single collider
            // This will make debris within a group collide some time after the explosion happens
            EntityManager.AddComponentData(instance, new ChangeFilterCountdown
            {
                Countdown = spawnSettings.Countdown * 2,
                Filter = oldFilter
            });

            // Make one collider unique for each spawned explosion group
            collider.MakeUnique(instance, EntityManager);

            // Set the GroupIndex to GroupId, which is negative
            // This ensures that the debris within a group doesn't collide
            collider.Value.Value.SetCollisionFilter(new CollisionFilter
            {
                BelongsTo = oldFilter.BelongsTo,
                CollidesWith = oldFilter.CollidesWith,
                GroupIndex = GroupId
            });

            GroupCollider = collider;
        }

        // Apply the updated collider data to all debris in the explosion group
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
