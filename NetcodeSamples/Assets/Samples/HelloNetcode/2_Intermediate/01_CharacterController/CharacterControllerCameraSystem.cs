using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    partial struct CharacterControllerCameraSystem : ISystem
    {
        public static readonly float3 k_CameraOffset = new float3(0, 2, -5);

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableCharacterController>();
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<Character>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var camera = UnityEngine.Camera.main;
            //We need to access the LocalToWorld matrix to match the position of the player in term of presentation.
            //Because Physics can be either Interpolated or Predicted, we the LocalToWorld can be different than the real world position
            //of the entity.
            foreach (var (localToWorld, input) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<CharacterControllerPlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                camera.transform.rotation = math.mul(quaternion.RotateY(input.ValueRO.Yaw), quaternion.RotateX(-input.ValueRO.Pitch));
                var offset = math.rotate(camera.transform.rotation, k_CameraOffset);
                camera.transform.position = localToWorld.ValueRO.Position + offset;
            }
        }
    }
}
