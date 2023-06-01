using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using UnityEngine;
using FloatRange = Unity.Physics.Math.FloatRange;

namespace Unity.Physics.Authoring
{
    // stores an initial value and a pair of scalar curves to apply to relevant constraints on the joint
    struct ModifyJointLimits : ISharedComponentData, IEquatable<ModifyJointLimits>
    {
        public PhysicsJoint InitialValue;
        public ParticleSystem.MinMaxCurve AngularRangeScalar;
        public ParticleSystem.MinMaxCurve LinearRangeScalar;

        public bool Equals(ModifyJointLimits other) =>
            AngularRangeScalar.Equals(other.AngularRangeScalar) && LinearRangeScalar.Equals(other.LinearRangeScalar);

        public override bool Equals(object obj) => obj is ModifyJointLimits other && Equals(other);

        public override int GetHashCode() =>
            unchecked((AngularRangeScalar.GetHashCode() * 397) ^ LinearRangeScalar.GetHashCode());
    }

    // an authoring component to add to a GameObject with one or more Joint
    public class ModifyJointLimitsAuthoring : MonoBehaviour
    {
        public ParticleSystem.MinMaxCurve AngularRangeScalar = new ParticleSystem.MinMaxCurve(
            1f,
            min: new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(2f, -2f, 0f, 0f),
                new Keyframe(4f, 0f, 0f, 0f)
            )
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            },
            max: new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(2f, -1f, 0f, 0f),
                new Keyframe(4f, 1f, 0f, 0f)
            )
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            }
        );

        public ParticleSystem.MinMaxCurve LinearRangeScalar = new ParticleSystem.MinMaxCurve(
            1f,
            min: new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(2f, 0.5f, 0f, 0f),
                new Keyframe(4f, 1f, 0f, 0f)
            )
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            },
            max: new AnimationCurve(
                new Keyframe(0f, 0.5f, 0f, 0f),
                new Keyframe(2f, 0f, 0f, 0f),
                new Keyframe(4f, 0.5f, 0f, 0f)
            )
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            }
        );
    }

    [BakingType]
    public class ModifyJointLimitsBakingData : IComponentData
    {
        public ParticleSystem.MinMaxCurve AngularRangeScalar;
        public ParticleSystem.MinMaxCurve LinearRangeScalar;
    }

    class ModifyJointLimitsBaker : Baker<ModifyJointLimitsAuthoring>
    {
        public override void Bake(ModifyJointLimitsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new ModifyJointLimitsBakingData
            {
                AngularRangeScalar = authoring.AngularRangeScalar,
                LinearRangeScalar = authoring.LinearRangeScalar
            });
        }
    }

    // after joints have been converted, find the entities they produced and add ModifyJointLimits to them
    [UpdateAfter(typeof(EndJointBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct ModifyJointLimitsBakingSystem : ISystem
    {
        private EntityQuery _ModifyJointLimitsBakingDataQuery;
        private EntityQuery _JointEntityBakingQuery;

        public void OnCreate(ref SystemState state)
        {
            _ModifyJointLimitsBakingDataQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<ModifyJointLimitsBakingData>()},
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });

            _JointEntityBakingQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<JointEntityBaking>()}
            });

            _ModifyJointLimitsBakingDataQuery.AddChangedVersionFilter(typeof(ModifyJointLimitsBakingData));
            _JointEntityBakingQuery.AddChangedVersionFilter(typeof(JointEntityBaking));
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_ModifyJointLimitsBakingDataQuery.IsEmpty && _JointEntityBakingQuery.IsEmpty)
            {
                return;
            }

            // Collect all the joints
            NativeParallelMultiHashMap<Entity, (Entity, PhysicsJoint)> jointsLookUp =
                new NativeParallelMultiHashMap<Entity, (Entity, PhysicsJoint)>(10, Allocator.TempJob);

            foreach (var(jointEntity, physicsJoint, entity) in SystemAPI
                     .Query<RefRO<JointEntityBaking>, RefRO<PhysicsJoint>>().WithEntityAccess()
                     .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
            {
                jointsLookUp.Add(jointEntity.ValueRO.Entity, (entity, physicsJoint.ValueRO));
            }

            foreach (var(modifyJointLimits, entity) in SystemAPI.Query<ModifyJointLimitsBakingData>()
                     .WithEntityAccess().WithOptions(EntityQueryOptions.IncludeDisabledEntities |
                         EntityQueryOptions.IncludePrefab))
            {
                var angularModification = new ParticleSystem.MinMaxCurve(
                    multiplier: math.radians(modifyJointLimits.AngularRangeScalar.curveMultiplier),
                    min: modifyJointLimits.AngularRangeScalar.curveMin,
                    max: modifyJointLimits.AngularRangeScalar.curveMax
                );

                foreach (var joint in jointsLookUp.GetValuesForKey(entity))
                {
                    state.EntityManager.SetSharedComponentManaged(joint.Item1, new ModifyJointLimits
                    {
                        InitialValue = joint.Item2,
                        AngularRangeScalar = angularModification,
                        LinearRangeScalar = modifyJointLimits.LinearRangeScalar
                    });
                }
            }

            jointsLookUp.Dispose();
        }
    }

    // apply an animated effect to the limits on supported types of joints
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PhysicsSystemGroup), OrderLast = true)]
    partial struct ModifyJointLimitsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var(joint, modification) in SystemAPI.Query<RefRW<PhysicsJoint>, ModifyJointLimits>())
            {
                var animatedAngularScalar = new FloatRange(
                    modification.AngularRangeScalar.curveMin.Evaluate(time),
                    modification.AngularRangeScalar.curveMax.Evaluate(time)
                );
                var animatedLinearScalar = new FloatRange(
                    modification.LinearRangeScalar.curveMin.Evaluate(time),
                    modification.LinearRangeScalar.curveMax.Evaluate(time)
                );

                // in each case, get relevant properties from the initial value based on joint type, and apply scalar
                switch (joint.ValueRW.JointType)
                {
                    // Custom type could be anything, so this demo just applies changes to all constraints
                    case JointType.Custom:
                        var constraints = modification.InitialValue.GetConstraints();
                        for (var i = 0; i < constraints.Length; i++)
                        {
                            var constraint = constraints[i];
                            var isAngular = constraint.Type == ConstraintType.Angular;
                            var scalar = math.select(animatedLinearScalar, animatedAngularScalar, isAngular);
                            var constraintRange = (FloatRange)(new float2(constraint.Min, constraint.Max) * scalar);
                            constraint.Min = constraintRange.Min;
                            constraint.Max = constraintRange.Max;
                            constraints[i] = constraint;
                        }

                        joint.ValueRW.SetConstraints(constraints);
                        break;
                    // other types have corresponding getters/setters to retrieve more meaningful data
                    case JointType.LimitedDistance:
                        var distanceRange = modification.InitialValue.GetLimitedDistanceRange();
                        joint.ValueRW.SetLimitedDistanceRange(distanceRange * (float2)animatedLinearScalar);
                        break;
                    case JointType.LimitedHinge:
                        var angularRange = modification.InitialValue.GetLimitedHingeRange();
                        joint.ValueRW.SetLimitedHingeRange(angularRange * (float2)animatedAngularScalar);
                        break;
                    case JointType.Prismatic:
                        var distanceOnAxis = modification.InitialValue.GetPrismaticRange();
                        joint.ValueRW.SetPrismaticRange(distanceOnAxis * (float2)animatedLinearScalar);
                        break;
                    // ragdoll joints are composed of two separate joints with different meanings
                    case JointType.RagdollPrimaryCone:
                        modification.InitialValue.GetRagdollPrimaryConeAndTwistRange(
                            out var maxConeAngle,
                            out var angularTwistRange
                        );
                        joint.ValueRW.SetRagdollPrimaryConeAndTwistRange(
                            maxConeAngle * animatedAngularScalar.Max,
                            angularTwistRange * (float2)animatedAngularScalar
                        );
                        break;
                    case JointType.RagdollPerpendicularCone:
                        var angularPlaneRange = modification.InitialValue.GetRagdollPerpendicularConeRange();
                        joint.ValueRW.SetRagdollPerpendicularConeRange(angularPlaneRange *
                            (float2)animatedAngularScalar);
                        break;
                    // remaining types have no limits on their Constraint atoms to meaningfully modify
                    case JointType.BallAndSocket:
                    case JointType.Fixed:
                    case JointType.Hinge:
                        break;
                }
            }
        }
    }
}
