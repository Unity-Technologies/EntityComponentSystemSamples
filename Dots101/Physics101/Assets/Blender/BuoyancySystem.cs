using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics.Extensions;

namespace Blender
{
    public partial struct BuoyancySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BuoyancyZone>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (buoyant, transform, velocity, mass) in
                     SystemAPI.Query<RefRO<Buoyancy>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<PhysicsMass>>())
            {
                float3 currentPos = transform.ValueRW.Position;

                float depth = buoyant.ValueRO.WaterLevel - currentPos.y;
                float buoyancyForce = depth * buoyant.ValueRO.BuoyancyForce * deltaTime;
                velocity.ValueRW.ApplyLinearImpulse(mass.ValueRO, new float3(0, buoyancyForce, 0));

                // apply water drag
                velocity.ValueRW.Linear *= 1.0f - buoyant.ValueRO.Drag * deltaTime;
            }
        }
    }
}