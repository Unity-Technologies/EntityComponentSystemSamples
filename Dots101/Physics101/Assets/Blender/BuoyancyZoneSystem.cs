using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Blender
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct BuoyancyZoneSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<BuoyancyZone>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // disable buoyancy for all cubes
            var buoyancyQuery = SystemAPI.QueryBuilder().WithAll<Buoyancy>().Build();
            state.EntityManager.SetComponentEnabled<Buoyancy>(buoyancyQuery, false);
                
            // for buoyant cubes inside the buoyancy zone, copy the buoyancy properties and enabled the buoyancy
            {
                // get trigger events
                var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
                sim.FinalJobHandle.Complete();

                foreach (var triggerEvent in sim.TriggerEvents)
                {
                    Entity cubeEntity;
                    Entity zoneEntity;

                    // determine which body is a buoyant cube and which is a zone 
                    if (SystemAPI.HasComponent<Buoyancy>(triggerEvent.EntityA) &&
                        SystemAPI.HasComponent<BuoyancyZone>(triggerEvent.EntityB))
                    {
                        cubeEntity = triggerEvent.EntityA;
                        zoneEntity = triggerEvent.EntityB;
                    }
                    else if (SystemAPI.HasComponent<Buoyancy>(triggerEvent.EntityB) &&
                             SystemAPI.HasComponent<BuoyancyZone>(triggerEvent.EntityA))
                    {
                        cubeEntity = triggerEvent.EntityB;
                        zoneEntity = triggerEvent.EntityA;
                    }
                    else
                    {
                        // skip because this event is not for a cube and a zone
                        continue;
                    }

                    var zone = SystemAPI.GetComponentRW<BuoyancyZone>(zoneEntity);
                    var cubeBuoyancy = SystemAPI.GetComponentRW<Buoyancy>(cubeEntity);

                    // copy the zone's Buoyancy data to the cube
                    cubeBuoyancy.ValueRW = zone.ValueRO.Buoyancy;
                    
                    // enable the cube's Buoyancy so that it will be made to float by the BuoyancySystem
                    SystemAPI.SetComponentEnabled<Buoyancy>(cubeEntity, true);
                }
            }
        }
    }
}