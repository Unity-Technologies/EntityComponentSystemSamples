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

        SimulationCallbacks.Callback callback = (ref ISimulation simulation, JobHandle inDeps) =>
        {
            inDeps.Complete();  // TODO: shouldn't be needed (jobify the below)

            SimulationData.Contacts.Iterator iterator = simulation.Contacts.GetIterator();
            while (iterator.HasItemsLeft())
            {
                ContactHeader manifold = iterator.GetNextContactHeader();
                bool bUpdateNormal = (manifold.BodyPair.BodyAIndex == surfaceRBIdx) || (manifold.BodyPair.BodyBIndex == surfaceRBIdx);

                float distanceScale = 1;
                if (bUpdateNormal)
                {
                    var newNormal = surfaceNormal;
                    distanceScale = math.dot(newNormal, manifold.Normal);

                    //<todo.eoin.hpi Feels pretty weird.
                    //<todo.eoin.hp Need to make this work if user has read a contact
                    iterator.SetManifoldNormal(newNormal);
                }
                for (int i = 0; i < manifold.NumContacts; i++)
                {
                    ContactPoint cp = iterator.GetNextContact();

                    if (bUpdateNormal)
                    {
                        cp.Distance *= distanceScale;
                        iterator.UpdatePreviousContact(cp);
                    }
                }
            }

            return inDeps;
        };

        modifiers.Dispose();

        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, callback);

        return inputDeps;
    }
}
