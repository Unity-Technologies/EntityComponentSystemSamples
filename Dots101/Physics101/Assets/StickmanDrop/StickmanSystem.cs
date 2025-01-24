using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace StickmanDrop
{
    // the system runs after each iteration of collision detection and the solver
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct StickmanSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<Breakable>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // get the impulse events
            var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
            
            // to access the impulse events on main thread, we must sync any outstanding physics sim jobs
            sim.FinalJobHandle.Complete();   
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            // An impulse event is generated when an impulse exceeds a  
            // joint's Break Force or Break Torque values.
            
            foreach (var impulseEvent in sim.ImpulseEvents)
            {
                // An impulse event is generated for both bodies connected by the joint.
                // So DestroyEntity will be called for each joint twice, but this is not a problem
                // because multiple destroy commands for the same entity in a single ECB is not an error. 
                ecb.DestroyEntity(impulseEvent.JointEntity);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}