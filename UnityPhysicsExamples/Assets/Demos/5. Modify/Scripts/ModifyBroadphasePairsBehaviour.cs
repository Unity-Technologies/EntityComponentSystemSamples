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
        SimulationCallbacks.Callback callback = (ref ISimulation simulation, JobHandle inDeps) =>
        {
            inDeps.Complete(); //<todo Needed to initialize our modifier

            return new DisablePairsJob
            {
                Bodies = m_PhysicsWorld.PhysicsWorld.Bodies,
                Motions = m_PhysicsWorld.PhysicsWorld.MotionVelocities,
                Iterator = simulation.BodyPairs.GetIterator()
            }.Schedule(inDeps);
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, callback);

        return inputDeps;
    }

    struct DisablePairsJob : IJob
    {
        public NativeSlice<RigidBody> Bodies;
        [ReadOnly] public NativeSlice<MotionVelocity> Motions;
        public SimulationData.BodyPairs.Iterator Iterator;

        public unsafe void Execute()
        {
            while (Iterator.HasPairsLeft())
            {
                BodyIndexPair pair = Iterator.NextPair();

                // Disable the pair if a box collides with a static object
                if ((Bodies[pair.BodyAIndex].Collider != null && Bodies[pair.BodyAIndex].Collider->Type == ColliderType.Box && pair.BodyBIndex >= Motions.Length)
                    || (Bodies[pair.BodyBIndex].Collider != null && Bodies[pair.BodyBIndex].Collider->Type == ColliderType.Box && pair.BodyAIndex >= Motions.Length))
                {
                    Iterator.DisableLastPair();
                }
            }
        }
    }
}
