using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial struct JointDeletionSystem : ISystem, ISystemStartStop
{
    int m_Counter;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DeleteJoints>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        m_Counter = 0;
    }

    public void OnStopRunning(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<PhysicsJoint, PhysicsConstrainedBodyPair>().Build();
        if (query.IsEmpty)
        {
            return;
        }
        // else:
        ++m_Counter;

        if (m_Counter == 60)
        {
            // delete all joints between pairs of entities that both have a DeleteJoints component
            var entityPairs = query.ToComponentDataArray<PhysicsConstrainedBodyPair>(Allocator.Temp);
            var joints = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < joints.Length; ++i)
            {
                var bodyPair = entityPairs[i];
                if (state.EntityManager.HasComponent<DeleteJoints>(bodyPair.EntityA) &&
                    state.EntityManager.HasComponent<DeleteJoints>(bodyPair.EntityB))
                {
                    state.EntityManager.DestroyEntity(joints[i]);
                }
            }
        }
    }
}
