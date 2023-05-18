using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public class VerifyBrokenJointAuthoring : MonoBehaviour {}

public struct VerifyBrokenJointTag : IComponentData {}

public class BreakableJointBaker : Baker<VerifyBrokenJointAuthoring>
{
    public override void Bake(VerifyBrokenJointAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<VerifyBrokenJointTag>(entity);
    }
}

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct VerifyBrokenJointSystem : ISystem
{
    private EntityQuery m_PhysicsJointQuery;
    private int m_FrameCount;
    private int m_numNonBreakableJoints;

    public void OnCreate(ref SystemState state)
    {
        m_FrameCount = 0;
        m_numNonBreakableJoints = 0;
        m_PhysicsJointQuery = state.GetEntityQuery(typeof(PhysicsJoint));
        state.RequireForUpdate<VerifyBrokenJointTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        m_FrameCount++;

        // Before anything happens, make sure that there are joints in the scene.
        if (m_FrameCount == 1)
        {
            NativeArray<PhysicsJoint> joints = m_PhysicsJointQuery.ToComponentDataArray<PhysicsJoint>(Unity.Collections.Allocator.Temp);
            Assert.IsTrue(joints.Length > 0);

            int numBreakableJoints = 0;
            for (int i = 0; i < joints.Length; i++)
            {
                PhysicsJoint joint = joints[i];

                if (ShouldJointBreak(joint))
                {
                    numBreakableJoints++;
                }
                else
                {
                    m_numNonBreakableJoints++;
                }
            }

            // Make sure that there are breakable and non breakable joints.
            Assert.IsTrue(m_numNonBreakableJoints > 0, "Found zero non breakable joints!");
            Assert.IsTrue(numBreakableJoints > 0, "Found zero breakable joints!");
        }

        // After a while, check if breakable joints are broken.
        // They get destroyed by DestroyBrokenJointsSystem
        if (m_FrameCount == 20)
        {
            NativeArray<PhysicsJoint> joints = m_PhysicsJointQuery.ToComponentDataArray<PhysicsJoint>(Unity.Collections.Allocator.Temp);

            Assert.IsTrue(joints.Length == m_numNonBreakableJoints, $"Expected: {m_numNonBreakableJoints} non breakable joints, but got {joints.Length}");

            for (int i = 0; i < joints.Length; i++)
            {
                PhysicsJoint joint = joints[i];
                Assert.IsFalse(ShouldJointBreak(joint), "Expected non breakable joint, but got breakable instead.");
            }
        }
    }

    private static bool ShouldJointBreak(PhysicsJoint joint)
    {
        var constraints = joint.GetConstraints();
        for (int i = 0; i < constraints.Length; i++)
        {
            var constraint = constraints[i];
            if (((constraint.Type == ConstraintType.Linear) || (constraint.Type == ConstraintType.Angular)) && math.any(math.abs(constraint.MaxImpulse) < 1.0f))
            {
                return true;
            }
        }

        return false;
    }
}
