using Unity.Burst;
using Unity.Entities;
using Unity.Physics;

namespace Blender
{
    public partial struct RotateBladeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Blade>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (bladeData, velocity) in 
                     SystemAPI.Query<RefRO<Blade>, RefRW<PhysicsVelocity>>())
            {
                velocity.ValueRW.Angular = bladeData.ValueRO.AngularVelocity;
            }
        }
    }
}