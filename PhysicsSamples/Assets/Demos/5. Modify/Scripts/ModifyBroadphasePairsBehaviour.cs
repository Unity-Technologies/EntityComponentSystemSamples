using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using System;
using Unity.Burst;

//<todo.eoin.usermod Rename to ModifyOverlappingBodyPairsComponentData?
public struct ModifyBroadphasePairs : IComponentData {}

public class ModifyBroadphasePairsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ModifyBroadphasePairs());
    }
}

// A system which configures the simulation step to disable certain broad phase pairs
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(StepPhysicsWorld))]
public partial class ModifyBroadphasePairsSystem : SystemBase
{
    EntityQuery m_PairModifierGroup;

    BuildPhysicsWorld m_PhysicsWorld;
    StepPhysicsWorld m_StepPhysicsWorld;

    protected override void OnCreate()
    {
        m_PhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

        m_PairModifierGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ModifyBroadphasePairs) }
        });
    }

    protected override void OnUpdate()
    {
        if (m_PairModifierGroup.CalculateEntityCount() == 0)
        {
            return;
        }

        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return;
        }

        // Add a custom callback to the simulation, which will inject our custom job after the body pairs have been created
        SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            inDeps.Complete(); //<todo Needed to initialize our modifier

            return new DisablePairsJob
            {
                Bodies = m_PhysicsWorld.PhysicsWorld.Bodies,
                Motions = m_PhysicsWorld.PhysicsWorld.MotionVelocities
            }.Schedule(simulation, ref world, Dependency);
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, callback);
    }

    [BurstCompile]
    struct DisablePairsJob : IBodyPairsJob
    {
        public NativeArray<RigidBody> Bodies;
        [ReadOnly] public NativeArray<MotionVelocity> Motions;

        public unsafe void Execute(ref ModifiableBodyPair pair)
        {
            // Disable the pair if a box collides with a static object
            int indexA = pair.BodyIndexA;
            int indexB = pair.BodyIndexB;
            if ((Bodies[indexA].Collider != null && Bodies[indexA].Collider.Value.Type == ColliderType.Box && indexB >= Motions.Length)
                || (Bodies[indexB].Collider != null && Bodies[indexB].Collider.Value.Type == ColliderType.Box && indexA >= Motions.Length))
            {
                pair.Disable();
            }
        }
    }
}
