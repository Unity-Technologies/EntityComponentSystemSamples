using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct ShootingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<Hit>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var collisionHistory = SystemAPI.GetSingleton<PhysicsWorldHistorySingleton>();
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var ghostComponentFromEntity = SystemAPI.GetComponentLookup<GhostInstance>();
            var localToWorldFromEntity = SystemAPI.GetComponentLookup<LocalToWorld>();
            var lagCompensationEnabledFromEntity = SystemAPI.GetComponentLookup<LagCompensationEnabled>();
            var predictingTick = networkTime.ServerTick;
            // Do not perform hit-scan when rolling back, only when simulating the latest tick
            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            foreach (var (character, interpolationDelay, hitComponent) in SystemAPI.Query<CharacterAspect, RefRO<CommandDataInterpolationDelay>, RefRW<Hit>>().WithAll<Simulate>())
            {
                if (character.Input.SecondaryFire.IsSet)
                {
                    hitComponent.ValueRW.Victim = character.Self;
                    hitComponent.ValueRW.Tick = predictingTick;
                    continue;
                }
                if (!character.Input.PrimaryFire.IsSet)
                {
                    continue;
                }

                // When we fetch the CollisionWorld for ServerTick T, we need to account for the fact that the user
                // raised this input sometime on the previous tick (render-frame, technically).
                const int additionalRenderDelay = 1;

                // Breakdown of timings:
                // - On the client, predicting ServerTick: 100 (for example)
                // - InterpolationDelay: 2 ticks
                // - Rendering Latency (assumption): 1 tick (likely more than 1 due to: double/triple buffering, pipelining, monitor refresh & draw latency)
                // - Client visually sees 97 (-1 for render latency, -2 for lag compensation)
                // - CommandDataInterpolationTick.Delay is a delta between CurrentCommand.Tick vs InterpolationTick, thus -2.
                //   I.e. InterpolationDelay is already accounted for.
                // - On the server, we process this input on ServerTick:100.
                // - CommandDataInterpolationTick.Delay:-2 = 98 (-2)
                // - So the server also needs to subtract the rendering delay to be consistent with what the client sees and queries against (97).
                var delay = lagCompensationEnabledFromEntity.HasComponent(character.Self)
                    ? interpolationDelay.ValueRO.Delay + additionalRenderDelay
                    : additionalRenderDelay;

                collisionHistory.GetCollisionWorldFromTick(predictingTick, delay, ref physicsWorld, out var collWorld, out var expectedTick, out var returnedTick);
                var didClamp = expectedTick != returnedTick; // ClientWorld shouldn't be clamping when calling GetCollisionWorldFromTick!
                if(state.WorldUnmanaged.IsClient()) UnityEngine.Debug.Assert(!didClamp);


                var cameraRotation = math.mul(quaternion.RotateY(character.Input.Yaw), quaternion.RotateX(-character.Input.Pitch));
                var offset = math.rotate(cameraRotation, CharacterControllerCameraSystem.k_CameraOffset);
                var cameraPosition = character.Transform.ValueRO.Position + offset;
                var forward = math.mul(cameraRotation, math.forward());
                var rayInput = new RaycastInput
                {
                    Start = cameraPosition + forward,
                    End = cameraPosition + forward * 1000,
                    Filter = CollisionFilter.Default
                };
                bool hit = collWorld.CastRay(rayInput, out var closestHit);

                if (!hit)
                {
                    continue;
                }

                var hitEntity = Entity.Null;
                var hitPoint = closestHit.Position;
                if (ghostComponentFromEntity.HasComponent(closestHit.Entity))
                {
                    hitEntity = closestHit.Entity;

                    var localToWorld = localToWorldFromEntity[hitEntity].Value;
                    hitPoint = math.mul(math.inverse(localToWorld), new float4(hitPoint, 1)).xyz;
                    //UnityEngine.Debug.Log($"<color=#FFFFAA>[{state.WorldUnmanaged.Name}] logged HIT on {predictingTick.ToFixedString()} (expected:{expectedTick.ToFixedString()}, actual/returned:{returnedTick.ToFixedString()}) with victim at worldPos:{collWorld.Bodies[closestHit.RigidBodyIndex].WorldFromBody.pos}!</color>");
                }

                hitComponent.ValueRW.Victim = hitEntity;
                hitComponent.ValueRW.HitPoint = hitPoint;
                hitComponent.ValueRW.Tick = predictingTick;
            }
        }
    }
}
