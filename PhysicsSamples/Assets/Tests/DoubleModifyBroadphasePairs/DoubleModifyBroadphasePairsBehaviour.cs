using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public struct DoubleModifyBroadphasePairs : IComponentData {}

public class DoubleModifyBroadphasePairsBehaviour : MonoBehaviour
{
    class DoubleModifyBroadphasePairsBaker : Baker<DoubleModifyBroadphasePairsBehaviour>
    {
        public override void Bake(DoubleModifyBroadphasePairsBehaviour authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<DoubleModifyBroadphasePairs>(entity);
        }
    }
}

// A system which configures the simulation step to disable certain broad phase pairs
[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
[UpdateBefore(typeof(PhysicsCreateContactsGroup))]
public partial struct DoubleModifyBroadphasePairsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(DoubleModifyBroadphasePairs) }
        }));
    }

    public void OnUpdate(ref SystemState state)
    {
        SimulationSingleton simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

        if (simSingleton.Type == SimulationType.NoPhysics)
        {
            return;
        }

        PhysicsWorldSingleton worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        // Add a custom callback to the simulation, which will inject our custom job after the body pairs have been created
        state.Dependency = new DisableDynamicDynamicPairsJob
        {
            NumDynamicBodies = worldSingleton.PhysicsWorld.NumDynamicBodies
        }.Schedule(simSingleton, ref worldSingleton.PhysicsWorld, state.Dependency);

        state.Dependency = new DisableDynamicStaticPairsJob
        {
            NumDynamicBodies = worldSingleton.PhysicsWorld.NumDynamicBodies
        }.Schedule(simSingleton, ref worldSingleton.PhysicsWorld, state.Dependency);
    }

    [BurstCompile]
    struct DisableDynamicDynamicPairsJob : IBodyPairsJob
    {
        public int NumDynamicBodies;

        public unsafe void Execute(ref ModifiableBodyPair pair)
        {
            // Disable the pair if it's dynamic-dynamic
            bool isDynamicDynamic = pair.BodyIndexA < NumDynamicBodies && pair.BodyIndexB < NumDynamicBodies;
            if (isDynamicDynamic)
            {
                pair.Disable();
            }
        }
    }

    [BurstCompile]
    struct DisableDynamicStaticPairsJob : IBodyPairsJob
    {
        public int NumDynamicBodies;

        public unsafe void Execute(ref ModifiableBodyPair pair)
        {
            // Disable the pair if it's dynamic-static
            bool isDynamicStatic = (pair.BodyIndexA < NumDynamicBodies && pair.BodyIndexB >= NumDynamicBodies) ||
                (pair.BodyIndexB < NumDynamicBodies && pair.BodyIndexA >= NumDynamicBodies);
            if (isDynamicStatic)
            {
                pair.Disable();
            }
        }
    }
}
