using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using System;

//<todo.eoin.usermod Rename to ModifyOverlappingBodyPairsComponentData?
public struct ModifyBroadphasePairs : IComponentData { }

public class ModifyBroadphasePairsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ModifyBroadphasePairs());
    }
}

// A system which configures the simulation step to disable certain broad phase pairs
[UpdateBefore(typeof(StepPhysicsWorld))]
public class ModifyBroadphasePairsSystem : JobComponentSystem
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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PairModifierGroup.CalculateLength() == 0)
        {
            return inputDeps;
        }

        if( m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics )
        {
            return inputDeps;
        }

        // Add a custom callback to the simulation, which will inject our custom job after the body pairs have been created
        SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            inDeps.Complete(); //<todo Needed to initialize our modifier

            return new DisablePairsJob
            {
                Bodies = m_PhysicsWorld.PhysicsWorld.Bodies,
                Motions = m_PhysicsWorld.PhysicsWorld.MotionVelocities
            }.Schedule(simulation, ref world, inputDeps);
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, callback);

        return inputDeps;
    }

    struct DisablePairsJob : IBodyPairsJob
    {
        public NativeSlice<RigidBody> Bodies;
        [ReadOnly] public NativeSlice<MotionVelocity> Motions;

        public unsafe void Execute(ref ModifiableBodyPair pair)
        {
            // Disable the pair if a box collides with a static object
            int indexA = pair.BodyIndices.BodyAIndex;
            int indexB = pair.BodyIndices.BodyBIndex;
            if ((Bodies[indexA].Collider != null && Bodies[indexA].Collider->Type == ColliderType.Box && indexB >= Motions.Length)
                || (Bodies[indexB].Collider != null && Bodies[indexB].Collider->Type == ColliderType.Box && indexA >= Motions.Length))
            {
                pair.Disable();
            }
        }
    }
}
