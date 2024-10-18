using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

// This system is used by the Runtime Collision Filter Modification demo to change the collision filter of the static
// cubes in the scene. The spheres will fall through the cubes and collide depending on what the CollisionFilter
// CollidersWith value is set. The cubes will change colour (material) to match the CollisionFilter CollidesWith value.
// This demo relies on there being only 3 CollisionFilters Categories used and they must be sequential.
// The Physics Category Names that must be set are:
// - Category 20 = Red (value = 1 << 20, 1048576)
// - Category 21 = Green (value = 1 << 21, 2097152)
// - Category 22 = Blue (value = 1 << 22, 4194304)
[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial class RotateThroughCollisionFiltersSystem : SystemBase
{
    private const uint RedCollisionFilter = 1 << 20;
    private const uint GreenCollisionFilter = 1 << 21;
    private const uint BlueCollisionFilter = 1 << 22;

    protected override void OnCreate()
    {
        RequireForUpdate<ChangeCollisionFilterCountdown>();
    }

    protected override void OnStartRunning()
    {
        var collisionFilterQuery = SystemAPI.QueryBuilder().WithAll<ChangeCollisionFilterCountdown>().Build();
        var entityArray = collisionFilterQuery.ToEntityArray(Allocator.Temp);
        if (entityArray.Length == 0) return;

        // Get the RenderMeshArray from the first entity in the query and use it to find the Material indices that
        // match the Materials of the static cubes in the scene
        var mesh = EntityManager.GetSharedComponentManaged<RenderMeshArray>(entityArray[0]);

        // For each red/green/blue GameObject, find the matching Material index in the RenderMeshArray
        var indexRed = FindMatchingMaterialIndex("Red", ref mesh);
        var indexBlue = FindMatchingMaterialIndex("Blue", ref mesh);
        var indexGreen = FindMatchingMaterialIndex("Green", ref mesh);

        // Save the Material indices in a singleton component for later use
        var colours = new ColoursForFilter
        {
            RedIndex = indexRed,
            BlueIndex = indexBlue,
            GreenIndex = indexGreen
        };
        Entity coloursEntity = EntityManager.CreateEntity(typeof(ColoursForFilter));
        EntityManager.SetComponentData(coloursEntity, colours);
        SystemAPI.SetSingleton(colours);

        entityArray.Dispose();
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.TryGetSingleton(out ColoursForFilter colourIndices))
            return;

        // Change the CollisionFilter of the static cubes
        var jobHandle = new RotateFilterCountDownJob()
            .Schedule(Dependency);

        Dependency = jobHandle;
        jobHandle.Complete();

        // Change the Material(colour) of the colliders based on their CollisionFilter
        foreach (var(collider, _, countdown, entity)
                 in SystemAPI.Query<RefRO<PhysicsCollider>, RenderMeshArray, RefRO<ChangeCollisionFilterCountdown>>()
                     .WithEntityAccess()
                     .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
        {
            if (countdown.ValueRO.Countdown == countdown.ValueRO.ResetCountdown) // Material will need an update
            {
                // Get the modified collision filter
                var filter = collider.ValueRO.Value.Value.GetCollisionFilter();

                // Update the material based on the matching collision filter
                int index = -1;
                if (filter.CollidesWith == RedCollisionFilter)
                {
                    index = colourIndices.RedIndex;
                }
                else if (filter.CollidesWith == GreenCollisionFilter)
                {
                    index = colourIndices.GreenIndex;
                }
                else if (filter.CollidesWith == BlueCollisionFilter)
                {
                    index = colourIndices.BlueIndex;
                }

                if (index > -1)
                {
                    var newMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(entity);
                    newMeshInfo.Material = MaterialMeshInfo.ArrayIndexToStaticIndex(index);
                    EntityManager.SetComponentData(entity, newMeshInfo);
                }
            }
        }
    }

    // This job counts down the ChangeCollisionFilterCountdown component and changes the collision filter when the
    // countdown reaches zero.
    [BurstCompile]
    private partial struct RotateFilterCountDownJob : IJobEntity
    {
        private void Execute(ref PhysicsCollider collider, ref ChangeCollisionFilterCountdown tag)
        {
            if (--tag.Countdown > 0) return;

            tag.Countdown = tag.ResetCountdown; //reset the countdown
            ref var colliderBlob = ref collider.Value.Value;

            if (!colliderBlob.IsUnique)
            {
                Debug.LogWarning($"Warning: The collider {colliderBlob.Type} is not unique. This will change the filter on all shared collider blobs.");
            }

            var currentFilter = colliderBlob.GetCollisionFilter();
            uint filter = currentFilter.CollidesWith;
            uint newValue = filter << 1; // bit shift to get the next filter
            if (newValue > BlueCollisionFilter) newValue = RedCollisionFilter; // roll around to the first filter
            CollisionFilter newFilter = new CollisionFilter
            {
                BelongsTo = newValue,
                CollidesWith = newValue,
                GroupIndex = currentFilter.GroupIndex
            };
            colliderBlob.SetCollisionFilter(newFilter);
        }
    }

    // This method searches through the input RenderMeshArray Materials[] array for the material with the matching name
    // and returns the index of the matching material
    private int FindMatchingMaterialIndex(string name, ref RenderMeshArray inputRenderMeshArray)
    {
        bool match = false;
        int index = -1;
        while (!match)
        {
            ++index;
            var compareMaterial = inputRenderMeshArray.MaterialReferences[index].Value.name;
            if (compareMaterial.Equals(name))
            {
                match = true;
            }
        }

        return index;
    }
}
