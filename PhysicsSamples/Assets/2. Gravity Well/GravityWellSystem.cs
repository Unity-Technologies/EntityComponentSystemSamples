using System.Collections.ObjectModel;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Burst;

namespace Conversion
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct GravityWellSystem : ISystem
    {
        private EntityQuery gravityWellQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            gravityWellQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld>().WithAllRW<GravityWell>().Build();
            // Only need to update the GravityWellSystem if there are any entities with a GravityWellComponent
            state.RequireForUpdate(gravityWellQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gravityWells = CollectionHelper.CreateNativeArray<GravityWell>(
                gravityWellQuery.CalculateEntityCount(), state.WorldUpdateAllocator,
                NativeArrayOptions.UninitializedMemory);

            // For each gravity well component, update the position and add them to the array
            new GravityWellJob
            {
                GravityWells = gravityWells
            }.ScheduleParallel();

            new DynamicBodiesJob
            {
                GravityWells = gravityWells,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct GravityWellJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeArray<GravityWell> GravityWells;

            public void Execute([EntityIndexInQuery] int entityIndexInQuery, ref GravityWell gravityWell,
                in LocalToWorld transform)
            {
                gravityWell.Position = transform.Position;
                GravityWells[entityIndexInQuery] = gravityWell;
            }
        }

        [BurstCompile]
        public partial struct DynamicBodiesJob : IJobEntity
        {
            [ReadOnly][NativeDisableParallelForRestriction]
            public NativeArray<GravityWell> GravityWells;

            public float DeltaTime;

            public void Execute(ref PhysicsVelocity velocity, in PhysicsCollider collider, in PhysicsMass mass,
                in LocalTransform localTransform)
            {
                for (int i = 0; i < GravityWells.Length; i++)
                {
                    var gravityWell = GravityWells[i];
                    velocity.ApplyExplosionForce(
                        mass, collider, localTransform.Position, localTransform.Rotation,
                        -gravityWell.Strength, gravityWell.Position, gravityWell.Radius,
                        DeltaTime, math.up());
                }
            }
        }
    }
}
