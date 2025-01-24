using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Elevator
{
    public partial struct ElevatorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Elevator>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (elevator, trans, velocity) in
                     SystemAPI.Query<RefRW<Elevator>, RefRO<LocalTransform>, RefRW<PhysicsVelocity>>())
            {
                // if not moving...
                if (velocity.ValueRW.Linear.y == 0)
                {
                    // go up
                    velocity.ValueRW.Linear.y = elevator.ValueRO.Speed;
                }
                // if going up...
                else if (velocity.ValueRW.Linear.y > 0)
                {
                    // if hit top...
                    if (trans.ValueRO.Position.y > elevator.ValueRO.MaxHeight)
                    {
                        // go down
                        velocity.ValueRW.Linear.y = -elevator.ValueRO.Speed;
                    }
                }
                // if going down...
                else
                {
                    // if hit bottom...
                    if (trans.ValueRO.Position.y < elevator.ValueRO.MinHeight)
                    {
                        // go up
                        velocity.ValueRW.Linear.y = elevator.ValueRO.Speed;
                    }
                }
            }
        }
    }
}