#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Hybrid;
using Unity.Transforms;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Run this system after collecting input but before Animator.Update is called.
    /// As Animator.Update is invoked right after SimulationSystemGroup,
    /// we inject our selves right after this system group runs.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UpdateAnimationStateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableAnimation>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var presentationGameObjectSystem =
                state.World.GetExistingSystemManaged<GhostPresentationGameObjectSystem>();
            foreach (var (input, localToWorld, character, entity) in SystemAPI
                         .Query<RefRO<CharacterControllerPlayerInput>, RefRW<LocalTransform>, RefRO<Character>>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                var gameObjectForEntity =
                    presentationGameObjectSystem.GetGameObjectForEntity(state.EntityManager, entity);
                var myData = new CharacterAnimationData
                {
                    IsShooting = input.ValueRO.PrimaryFire.IsSet,
                    Movement = input.ValueRO.Movement,
                    OnGround = character.ValueRO.OnGround == 1,
                    Pitch = input.ValueRO.Pitch,
                    Yaw = input.ValueRO.Yaw,
                };
                var characterAnimation = gameObjectForEntity.GetComponent<CharacterAnimation>();
                if (characterAnimation != null)
                {
                    localToWorld.ValueRW = characterAnimation.UpdateAnimationState(myData, localToWorld.ValueRO);
                }
            }
        }
    }
}
#endif
