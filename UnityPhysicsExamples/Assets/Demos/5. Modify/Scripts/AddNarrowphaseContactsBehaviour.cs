using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;
using ContactPoint = Unity.Physics.ContactPoint;
using SphereCollider = Unity.Physics.SphereCollider;

public struct AddNarrowphaseContacts : IComponentData { }

public class AddNarrowphaseContactsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AddNarrowphaseContacts());
    }
}

// A system which configures the simulation step to inject extra user defined contact points
[UpdateBefore(typeof(StepPhysicsWorld))]
public class AddContactsSystem : JobComponentSystem
{
    EntityQuery m_ContactAdderGroup;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorld;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

        m_ContactAdderGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(AddNarrowphaseContacts) }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if(m_ContactAdderGroup.CalculateLength() == 0)
        {
            return inputDeps;
        }

        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return inputDeps;
        }

        // Enqueue a callback which will inject a job into the simulation step
        SimulationCallbacks.Callback callback = (ref ISimulation simulation, JobHandle inDeps) =>
        {
            inDeps.Complete();  // TODO: shouldn't be needed?

            JobHandle handle = new AddContactsJob
            {
                Bodies = m_BuildPhysicsWorldSystem.PhysicsWorld.Bodies,
                Motions = m_BuildPhysicsWorldSystem.PhysicsWorld.MotionDatas,
                Contacts = simulation.Contacts
            }.Schedule(inDeps);

            handle.Complete(); //<todo.eoin.usermod Remove. Difficult, due to deallocation of schedulerInfo.

            return handle;
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, callback);

        return inputDeps;
    }

    struct AddContactsJob : IJob
    {
        [ReadOnly] public NativeSlice<RigidBody> Bodies;
        [ReadOnly] public NativeSlice<MotionData> Motions;
        public SimulationData.Contacts Contacts;

        public unsafe void Execute()
        {
            // Add a contact to any dynamic spheres
            for (int m = 0; m < Motions.Length; m++)
            {
                if (Bodies[m].Collider->Type == ColliderType.Sphere)
                {
                    var sc = (SphereCollider*)Bodies[m].Collider;
                    float3 bodyPos = Bodies[m].WorldFromBody.pos;

                    var ch = new ContactHeader();
                    ch.BodyPair.BodyAIndex = m;
                    ch.BodyPair.BodyBIndex = Motions.Length;
                    ch.NumContacts = 1;
                    ch.Normal = new float3(0, 1, 0);

                    var cp = new ContactPoint
                    {
                        Distance = math.dot(ch.Normal, bodyPos) - sc->Radius,
                        Position = bodyPos - ch.Normal * math.dot(ch.Normal, bodyPos)
                    };

                    Contacts.AddContact(ch, cp);
                }
            }

            Contacts.CommitAddedContacts();
        }
    }
}

/*

    BlockStream(int n) -> Create a blockstream with n ranges
        ForEachCount -> Number of ranges

    MultiBlockStream(int n) -> Creates a block stream with n "blocklists"
        NumWorkItems -> sum of ReaderInfo.NumWorkItemsPerPhase
        NumPhases -> num blocklists
        NumWorkItemsPerPhase(p) -> ReaderInfo[p].NumWorkItemsPerPhase
        FirstWorkItemsPerPhase(p) -> ReaderInfo[p].FirstWorkItemIndex


    Systems.FindOverlappingBodies.OnUpdate
        Schedule ProducePhasedContactPairsJob
            Input of sortedBodyPairs array
            Output physicsContext.PhasedCollisionPairs
            Single-threaded

            foreach(pair) : MultiBlockStreamWriter.Write( pair, calcPhaseId(pair) )
                -> Increases ElementCount in blocklist[ phaseId ]
            MultiBlockStreamWriter.WriteReadersInfo(numWorkerThreads * 2, _batchSize, numPairs);
                for( n in numBlockLists ):
                    Set readerInfo[n]:
                        ItemsPerPhase = blockList[n].ElementCount
                        BatchSizePerPhase = min(_batchSize, blocklist[n].ElementCount)
                        NumWorkItemsPerPhase = blockList[n].ElementCount / BatchSizePerPhase
                        FirstWorkItemIndex = sum( readerInfo[0 : n-1].NumWorkItemsPerPhase )
                workItemCount = sum( blockLists[].ElementCount )


    Systems.CreateContacts.OnUpdate
        Schedule ProcessBodyPairsJob
            Iteration count = PhasedCollisionPairs/MultiBlockStream.NumWorkItems
                -> iter becomes a batch; multiple batches per-phase
            numPairs = MultiBlockStreamReader.StartWorkItem(iter)
                -> Returns batch size or number of elements in last batch
            physicsContext.Conacts/BlockStream.Writer.BeginForEachIndex(iter)
            Read pair from MBSR, write contact point to BSW


        Schedule DisposeMultiBlockStreamJob
            Frees PhasedCollisionPairs blocks + readerInfo


    Systems.CreateContactJacobians.OnUpdate
        Schedule BuildJacobiansJob
            Iteration count = PhasedCollisionPairs/MultiBlockStream.NumWorkItems
                -> iter becomes a batch; multiple batches per-phase
            numToBuild = physicsContext.Contacts/BlockStream.Reader.BeginForEachIndex(iter)
            physicsContext.Jacobians/BlockStream.Writer.BeginForEachIndex(iter)


    Systems.SolveContactJacobians.OnUpdate
        Schedule SolverJob:
            foreach(phase : PhasedCollisionPairs/MultiBlockStream.NumPhases)
                numWorkItems = PhasedCollisionPairs/MultiBlockStream.NumWorkItemsPerPhase(phase)
                Schedule SolverTask with NumWorkItems batchSize and iter count
                    workItemStartIndexOffset = PhasedCollisionPairs/MultiBlockStream.FirstWorkItemsPerPhase(phase)
                Seems to create single-threaded chain of tasks?
                !!! Last job has a batchsize of 1. Is this not a race condition?

            SolverJob:
                numToProcess = physicsContext.Jacobians/BlockStreamReader.BeginForEachIndex(iter + workItemStartIndexOffset)


        Schedule DisposeJacobiansJob


*/
