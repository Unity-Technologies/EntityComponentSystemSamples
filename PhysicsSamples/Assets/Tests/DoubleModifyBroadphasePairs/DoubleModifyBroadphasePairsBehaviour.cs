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
            AddComponent<DoubleModifyBroadphasePairs>();
        }
    }
}

// A system which configures the simulation step to disable certain broad phase pairs
[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
[UpdateBefore(typeof(PhysicsCreateContactsGroup))]
public partial class DoubleModifyBroadphasePairsSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(DoubleModifyBroadphasePairs) }
        }));
    }

    protected override void OnUpdate()
    {
        SimulationSingleton simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

        if (simSingleton.Type == SimulationType.NoPhysics)
        {
            return;
        }

        PhysicsWorldSingleton worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        // Add a custom callback to the simulation, which will inject our custom job after the body pairs have been created
        Dependency = new DisableDynamicDynamicPairsJob
        {
            NumDynamicBodies = worldSingleton.PhysicsWorld.NumDynamicBodies
        }.Schedule(simSingleton, ref worldSingleton.PhysicsWorld, Dependency);

        Dependency = new DisableDynamicStaticPairsJob
        {
            NumDynamicBodies = worldSingleton.PhysicsWorld.NumDynamicBodies
        }.Schedule(simSingleton, ref worldSingleton.PhysicsWorld, Dependency);
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
