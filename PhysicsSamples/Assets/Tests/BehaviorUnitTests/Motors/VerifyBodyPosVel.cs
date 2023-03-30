using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyBodyPosVelData : IComponentData
    {
        public float3 ExpectedPosition;
        public float3 ExpectedVelocity;
        public quaternion ExpectedOrientation;
        public float3 ExpectedAngularVelocity;

        public int StartAtFrame;
        public float Tolerance;

        public bool CheckPosition;
        public bool CheckVelocity;
        public bool CheckOrientation;
        public bool CheckAngularVelocity;
    }

    public class VerifyBodyPosVel : MonoBehaviour
    {
        public int StartAtFrame;
        public float Tolerance = 0.01f;

        public bool CheckPosition;
        public float3 ExpectedPosition;
        public bool CheckVelocity;
        public float3 ExpectedVelocity;
        public bool CheckOrientation;
        public float3 ExpectedAxisOfRotation;
        public float ExpectedAngle;
        public bool CheckAngularVelocity;
        public float3 ExpectedAngularVelocity;

        class VerifyBodyPosVelBaker : Baker<VerifyBodyPosVel>
        {
            public override void Bake(VerifyBodyPosVel authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VerifyBodyPosVelData()
                {
                    ExpectedPosition = authoring.ExpectedPosition,
                    ExpectedVelocity = authoring.ExpectedVelocity,
                    ExpectedAngularVelocity = authoring.ExpectedAngularVelocity,
                    ExpectedOrientation = quaternion.AxisAngle(authoring.ExpectedAxisOfRotation, math.radians(authoring.ExpectedAngle)),
                    StartAtFrame = authoring.StartAtFrame,
                    Tolerance = authoring.Tolerance,
                    CheckPosition = authoring.CheckPosition,
                    CheckVelocity = authoring.CheckVelocity,
                    CheckOrientation = authoring.CheckOrientation,
                    CheckAngularVelocity = authoring.CheckAngularVelocity
                });
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct VerifyBodyPosVelSystem : ISystem
    {
        EntityQuery m_VerificationGroup;
        int m_FrameCount;

        public void OnCreate(ref SystemState state)
        {
            m_FrameCount = -1;
            state.RequireForUpdate<VerifyBodyPosVelData>();
            m_VerificationGroup = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyBodyPosVelData) }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            m_FrameCount++;
            var entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                VerifyBodyPosVelData data = state.EntityManager.GetComponentData<VerifyBodyPosVelData>(entity);
                if (data.StartAtFrame > m_FrameCount)
                {
                    // Desired frame not reached yet
                    continue;
                }

                if (data.CheckPosition)
                {
                    var translation = state.EntityManager.GetComponentData<LocalTransform>(entity).Position;
                    Assert.IsTrue(math.distance(translation, data.ExpectedPosition) <= data.Tolerance,
                        $"{m_FrameCount}: Actual position {translation} of Entity {entity.Index} is not close enough to expected position {data.ExpectedPosition}");
                }

                if (data.CheckVelocity || data.CheckAngularVelocity)
                {
                    var vel = state.EntityManager.GetComponentData<PhysicsVelocity>(entity);
                    Assert.IsTrue(data.CheckVelocity ? math.distance(vel.Linear, data.ExpectedVelocity) <= data.Tolerance : true,
                        $"{m_FrameCount}: Actual linear velocity {vel.Linear} of Entity {entity.Index} is not close enough to expected one {data.ExpectedVelocity}");
                    if (data.CheckAngularVelocity)
                    {
                        var mass = state.EntityManager.GetComponentData<PhysicsMass>(entity);
                        quaternion rot = state.EntityManager.GetComponentData<LocalTransform>(entity).Rotation;

                        var angVelWorldSpace = vel.GetAngularVelocityWorldSpace(mass, rot);
                        var expectedRadians = math.radians(data.ExpectedAngularVelocity);
                        Assert.IsTrue(math.distance(angVelWorldSpace, expectedRadians) <= data.Tolerance,
                            $"{m_FrameCount}: Actual angular velocity {angVelWorldSpace} of Entity {entity.Index} is not close enough to expected one {expectedRadians}");
                    }
                }

                if (data.CheckOrientation)
                {
                    var rot = state.EntityManager.GetComponentData<LocalTransform>(entity).Rotation;

                    // Before comparing orientations, make sure we properly handle 180 deg rotations around an axis (it could be 180 around axis or -180 around -axis).
                    Assert.IsTrue(math.distance(AbsIfHalfCircle(rot).value, AbsIfHalfCircle(data.ExpectedOrientation).value) <= data.Tolerance,
                        $"{m_FrameCount}: Actual orientation {rot} of Entity {entity.Index} is not close enough to expected one {data.ExpectedOrientation}");
                }
            }
            entities.Dispose();
        }

        /// <summary>
        /// If the specified quaternion is 180 degrees around some axis, returns its absolute value (abs of each component)
        /// </summary>
        private static quaternion AbsIfHalfCircle(quaternion q)
        {
            const float eps = 0.0001f;
            return ((math.abs(q.value.x) >= 1.0f - eps) || (math.abs(q.value.z) >= 1.0f - eps) || (math.abs(q.value.z) >= 1.0f - eps)) ?
                new quaternion(math.abs(q.value.x), math.abs(q.value.y), math.abs(q.value.z), q.value.w) : q;
        }
    }
}
