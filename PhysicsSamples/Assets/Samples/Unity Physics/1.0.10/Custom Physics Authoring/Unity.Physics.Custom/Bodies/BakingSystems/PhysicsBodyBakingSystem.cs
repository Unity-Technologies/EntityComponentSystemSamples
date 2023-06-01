using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.GraphicsIntegration;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    [TemporaryBakingType]
    public struct PhysicsBodyAuthoringData : IComponentData
    {
        public bool IsDynamic;
        public float Mass;
        public bool OverrideDefaultMassDistribution;
        public MassDistribution CustomMassDistribution;
    }

    class PhysicsBodyAuthoringBaker : BasePhysicsBaker<PhysicsBodyAuthoring>
    {
        internal List<UnityEngine.Collider> colliderComponents = new List<UnityEngine.Collider>();
        internal List<PhysicsShapeAuthoring> physicsShapeComponents = new List<PhysicsShapeAuthoring>();

        public override void Bake(PhysicsBodyAuthoring authoring)
        {
            // Priority is to Legacy Components. Ignore if baked by Legacy.
            if (GetComponent<Rigidbody>()  || GetComponent<UnityEngine.Collider>())
            {
                return;
            }

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            // To process later in the Baking System
            AddComponent(entity, new PhysicsBodyAuthoringData
            {
                IsDynamic = (authoring.MotionType == BodyMotionType.Dynamic),
                Mass = authoring.Mass,
                OverrideDefaultMassDistribution = authoring.OverrideDefaultMassDistribution,
                CustomMassDistribution = authoring.CustomMassDistribution
            });

            AddSharedComponent(entity, new PhysicsWorldIndex(authoring.WorldIndex));

            var bodyTransform = GetComponent<Transform>();

            var motionType = authoring.MotionType;
            var hasSmoothing = authoring.Smoothing != BodySmoothing.None;

            PostProcessTransform(bodyTransform, motionType);

            var customTags = authoring.CustomTags;
            if (!customTags.Equals(CustomPhysicsBodyTags.Nothing))
                AddComponent(entity, new PhysicsCustomTags { Value = customTags.Value });

            // Check that there is at least one collider in the hierarchy to add these three
            GetComponentsInChildren(colliderComponents);
            GetComponentsInChildren(physicsShapeComponents);
            if (colliderComponents.Count > 0 || physicsShapeComponents.Count > 0)
            {
                AddComponent(entity, new PhysicsCompoundData()
                {
                    AssociateBlobToBody = false,
                    ConvertedBodyInstanceID = authoring.GetInstanceID(),
                    Hash = default,
                });
                AddComponent<PhysicsRootBaked>(entity);
                AddComponent<PhysicsCollider>(entity);
                AddBuffer<PhysicsColliderKeyEntityPair>(entity);
            }

            if (authoring.MotionType == BodyMotionType.Static || IsStatic())
                return;

            var massProperties = MassProperties.UnitSphere;

            AddComponent(entity, authoring.MotionType == BodyMotionType.Dynamic ?
                PhysicsMass.CreateDynamic(massProperties, authoring.Mass) :
                PhysicsMass.CreateKinematic(massProperties));

            var physicsVelocity = new PhysicsVelocity
            {
                Linear = authoring.InitialLinearVelocity,
                Angular = authoring.InitialAngularVelocity
            };
            AddComponent(entity, physicsVelocity);

            if (authoring.MotionType == BodyMotionType.Dynamic)
            {
                // TODO make these optional in editor?
                AddComponent(entity, new PhysicsDamping
                {
                    Linear = authoring.LinearDamping,
                    Angular = authoring.AngularDamping
                });
                if (authoring.GravityFactor != 1)
                {
                    AddComponent(entity, new PhysicsGravityFactor
                    {
                        Value = authoring.GravityFactor
                    });
                }
            }
            else if (authoring.MotionType == BodyMotionType.Kinematic)
            {
                AddComponent(entity, new PhysicsGravityFactor
                {
                    Value = 0
                });
            }

            if (hasSmoothing)
            {
                AddComponent(entity, new PhysicsGraphicalSmoothing());
                if (authoring.Smoothing == BodySmoothing.Interpolation)
                {
                    AddComponent(entity, new PhysicsGraphicalInterpolationBuffer
                    {
                        PreviousTransform = Math.DecomposeRigidBodyTransform(bodyTransform.localToWorldMatrix),
                        PreviousVelocity = physicsVelocity,
                    });
                }
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(EndColliderBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial class PhysicsBodyBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Fill in the MassProperties based on the potential calculated value by BuildCompoundColliderBakingSystem
            foreach (var(physicsMass, bodyData, collider) in
                     SystemAPI.Query<RefRW<PhysicsMass>, RefRO<PhysicsBodyAuthoringData>, RefRO<PhysicsCollider>>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                // Build mass component
                var massProperties = collider.ValueRO.MassProperties;
                if (bodyData.ValueRO.OverrideDefaultMassDistribution)
                {
                    massProperties.MassDistribution = bodyData.ValueRO.CustomMassDistribution;
                    // Increase the angular expansion factor to account for the shift in center of mass
                    massProperties.AngularExpansionFactor += math.length(massProperties.MassDistribution.Transform.pos - bodyData.ValueRO.CustomMassDistribution.Transform.pos);
                }

                physicsMass.ValueRW = bodyData.ValueRO.IsDynamic ?
                    PhysicsMass.CreateDynamic(massProperties, bodyData.ValueRO.Mass) :
                    PhysicsMass.CreateKinematic(massProperties);
            }
        }
    }
}
