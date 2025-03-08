using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;

namespace ActivationPlates
{
    // the system runs after collision detection and the solver
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct ActivationSystem : ISystem
    {
        public ulong physicsUpdateCount; // incremented for each physics update

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<ActivationPlates.Config>();
            physicsUpdateCount = 1; // start at 1 to prevent generating an erroneous Exit zone state in the first update
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            physicsUpdateCount++;

            // for zones that triggered events this update, set their state to Inside or Enter
            {
                // get trigger events
                var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
                sim.FinalJobHandle.Complete();

                foreach (var triggerEvent in sim.TriggerEvents)
                {
                    Entity playerEntity;
                    Entity zoneEntity;
                    
                    // determine which body is the player and which is a zone 
                    if (SystemAPI.HasComponent<Player>(triggerEvent.EntityA) &&
                        SystemAPI.HasComponent<Zone>(triggerEvent.EntityB))
                    {
                        playerEntity = triggerEvent.EntityA;
                        zoneEntity = triggerEvent.EntityB;
                    }
                    else if (SystemAPI.HasComponent<Player>(triggerEvent.EntityB) &&
                             SystemAPI.HasComponent<Zone>(triggerEvent.EntityA))
                    {
                        playerEntity = triggerEvent.EntityB;
                        zoneEntity = triggerEvent.EntityA;
                    }
                    else
                    {
                        // skip because this event is not for the player and a zone
                        continue;
                    }

                    var zone = SystemAPI.GetComponentRW<Zone>(zoneEntity);
                    zone.ValueRW.LastPhysicsUpdateCount = physicsUpdateCount;  // track when the zone was last entered

                    if (zone.ValueRO.State == ZoneState.Enter)
                    {
                        // was Enter, so now should be Inside
                        zone.ValueRW.State = ZoneState.Inside;
                    }
                    else if (zone.ValueRO.State == ZoneState.Exit ||
                             zone.ValueRO.State == ZoneState.Outside)
                    {
                        // was Exit or Outside, so now should be Enter
                        zone.ValueRW.State = ZoneState.Enter;
                    }
                }
            }

            // for zones that did NOT trigger an event this update, set their state to Exit or Outside
            {
                foreach (var zone in
                         SystemAPI.Query<RefRW<Zone>>())
                {
                    if (zone.ValueRO.LastPhysicsUpdateCount == physicsUpdateCount)
                    {
                        // skip because this zone generated a trigger event this update
                        continue;
                    }
                    
                    if (physicsUpdateCount - zone.ValueRO.LastPhysicsUpdateCount == 1)
                    {
                        // triggered an event in the prior update but not in this update
                        zone.ValueRW.State = ZoneState.Exit;
                    }
                    else
                    {
                        zone.ValueRW.State = ZoneState.Outside;
                    }
                }
            }

            // set color of the zones to green when entered, red when exited
            {
                foreach (var (zone, color) in
                         SystemAPI.Query<RefRW<Zone>, RefRW<URPMaterialPropertyBaseColor>>())
                {
                    if (zone.ValueRO.State == ZoneState.Enter)
                    {
                        color.ValueRW.Value = config.ActiveColor;
                    }
                    else if (zone.ValueRO.State == ZoneState.Exit)
                    {
                        color.ValueRW.Value = config.InactiveColor;
                    }
                }
            }

            // possibly spawn a box (depending upon zone state and zone type)
            {
                var spawnBox = false;

                foreach (var zone in
                         SystemAPI.Query<RefRW<Zone>>())
                {
                    var type = zone.ValueRO.Type;
                    var zoneState = zone.ValueRO.State;

                    if (type == ZoneType.OneTime && zoneState == ZoneState.Enter)
                    {
                        // if has not been previously entered
                        if (zone.ValueRO.LastTriggerTime == 0)
                        {
                            spawnBox = true;
                            zone.ValueRW.LastTriggerTime = (float)elapsedTime;
                        }
                    }
                    else if (type == ZoneType.Continuous && zoneState == ZoneState.Inside)
                    {
                        // if enough time has elapsed since last trigger
                        if (elapsedTime - zone.ValueRO.LastTriggerTime > config.ContinuousRepetitionInterval)
                        {
                            spawnBox = true;
                            zone.ValueRW.LastTriggerTime = (float)elapsedTime;
                        }
                    }
                    else if (type == ZoneType.Reenterable && zoneState == ZoneState.Enter)
                    {
                        spawnBox = true;
                    }
                    else if (type == ZoneType.OnExit && zoneState == ZoneState.Exit)
                    {
                        spawnBox = true;
                    }
                }

                if (spawnBox)
                {
                    state.EntityManager.Instantiate(config.SpawnPrefab);
                }
            }
        }
    }
}