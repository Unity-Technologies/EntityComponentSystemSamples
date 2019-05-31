using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;
using ContactPoint = Unity.Physics.ContactPoint;
using Unity.Physics.Extensions;

public struct ModifyNarrowphaseContacts : IComponentData
{
    public Entity surfaceEntity;
    public float3 surfaceNormal;
}

public class ModifyNarrowphaseContactsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject surfaceMesh = null;

    void OnEnable() { }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            dstManager.AddComponentData(entity, new ModifyNarrowphaseContacts()
            {
                surfaceEntity = entity,
                surfaceNormal = surfaceMesh.transform.up
            });
        }
    }
}

// A system which configures the simulation step to rotate certain contact normals
[UpdateBefore(typeof(StepPhysicsWorld))]
public class ModifyNarrowphaseContactsSystem : JobComponentSystem
{
    EntityQuery m_ContactModifierGroup;
    StepPhysicsWorld m_StepPhysicsWorld;
    BuildPhysicsWorld m_BuildPhysicsWorld;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

        m_ContactModifierGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ModifyNarrowphaseContacts) }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_ContactModifierGroup.CalculateLength() == 0)
        {
            return inputDeps;
        }

        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return inputDeps;
        }

        var modifiers = m_ContactModifierGroup.ToComponentDataArray<ModifyNarrowphaseContacts>(Allocator.TempJob);
        var surfaceNormal = modifiers[0].surfaceNormal;
        var surfaceRBIdx = m_BuildPhysicsWorld.PhysicsWorld.GetRigidBodyIndex(modifiers[0].surfaceEntity);

        SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            inDeps.Complete();  // TODO: shouldn't be needed (jobify the below)

            return new ModifyNormalsJob
            {
                m_SurfaceRBIdx = surfaceRBIdx,
                m_SurfaceNormal = surfaceNormal
            }.Schedule(simulation, ref world, inDeps);
        };

        modifiers.Dispose();

        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, callback);

        return inputDeps;
    }

    struct ModifyNormalsJob : IContactsJob
    {
        public int m_SurfaceRBIdx;
        public float3 m_SurfaceNormal;
        float distanceScale;

        public void Execute(ref ModifiableContactHeader contactHeader, ref ModifiableContactPoint contactPoint)
        {
            bool bUpdateNormal = (contactHeader.BodyIndexPair.BodyAIndex == m_SurfaceRBIdx) || (contactHeader.BodyIndexPair.BodyBIndex == m_SurfaceRBIdx);

            if (bUpdateNormal && contactPoint.Index == 0)
            {
                var newNormal = m_SurfaceNormal;
                distanceScale = math.dot(newNormal, contactHeader.Normal);

                contactHeader.Normal = newNormal;
            }

            if (bUpdateNormal)
            {
                contactPoint.Distance *= distanceScale;
            }
        }
    }
}
