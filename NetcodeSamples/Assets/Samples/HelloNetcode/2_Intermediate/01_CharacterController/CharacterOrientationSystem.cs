using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Samples.HelloNetcode
{
    [BurstCompile]
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation|WorldSystemFilterFlags.ServerSimulation)]
    public partial struct CharacterOrientationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Character>();
            state.RequireForUpdate<EnableCharacterOrientation>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            foreach (var character in SystemAPI.Query<CharacterAspect>().WithAll<Simulate>())
            {
                if (!character.AutoCommandTarget.Enabled)
                {
                    return;
                }

                var controllerConfig = SystemAPI.GetComponent<CharacterControllerConfig>(character.Character.ControllerConfig);

                float2 input = character.Input.Movement;
                float3 wantedMove = new float3(input.x, 0, input.y) * controllerConfig.Speed * SystemAPI.Time.DeltaTime;

                // Wanted movement is relative to camera
                wantedMove = math.rotate(quaternion.RotateY(character.Input.Yaw), wantedMove);

                // Character orientation; turn towards movement
                if (math.length(wantedMove) > 0)
                {
                    quaternion forw = quaternion.LookRotation(math.normalize(wantedMove), math.up());
                    character.Transform.ValueRW.Rotation = forw;
                }
            }
        }
    }
}
