using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public struct DoubleModifyBroadphasePairs : IComponentData {}

public class DoubleModifyBroadphasePairsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new DoubleModifyBroadphasePairs());
    }
}

// A system which configures the simulation step to disable certain broad phase pairs
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(StepPhysicsWorld))]
public class DoubleModifyBroadphasePairsSystem : SystemBase
{
    StepPhysicsWorld m_StepPhysicsWorld;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

        var pairModifierGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(DoubleModifyBroadphasePairs) }
        });

        RequireForUpdate(pairModifierGroup);
    }

    protected override void OnUpdate()
    {
        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return;
        }

        // Add a custom callback to the simulation, which will inject our custom job after the body pairs have been created
        SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            inDeps = new DisableDynamicDynamicPairsJob
            {
                NumDynamicBodies = world.NumDynamicBodies
            }.Schedule(m_StepPhysicsWorld.Simulation, ref world, inDeps);

            inDeps = new DisableDynamicStaticPairsJob
            {
                NumDynamicBodies = world.NumDynamicBodies
            }.Schedule(m_StepPhysicsWorld.Simulation, ref world, inDeps);

            return inDeps;
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, callback);
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
