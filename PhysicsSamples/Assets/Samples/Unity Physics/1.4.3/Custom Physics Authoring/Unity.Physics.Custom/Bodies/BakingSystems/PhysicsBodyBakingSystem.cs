using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
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
    public partial struct PhysicsBodyBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;

            // Fill in the mass properties based on custom mass properties for bodies without colliders
            foreach (var(physicsMass, bodyData, entity) in
                     SystemAPI.Query<RefRW<PhysicsMass>, RefRO<PhysicsBodyAuthoringData>>()
                         .WithNone<PhysicsCollider>()
                         .WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                physicsMass.ValueRW = CreatePhysicsMass(entityManager, entity, bodyData.ValueRO, MassProperties.UnitSphere);
            }

            // Fill in the mass properties based on collider and custom mass properties if provided.
            foreach (var(physicsMass, bodyData, collider, entity) in
                     SystemAPI.Query<RefRW<PhysicsMass>, RefRO<PhysicsBodyAuthoringData>, RefRO<PhysicsCollider>>()
                         .WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                physicsMass.ValueRW = CreatePhysicsMass(entityManager, entity, bodyData.ValueRO,
                    collider.ValueRO.MassProperties, true);
            }
        }

        private PhysicsMass CreatePhysicsMass(EntityManager entityManager, in Entity entity,
            in PhysicsBodyAuthoringData inBodyData, in MassProperties inMassProperties, in bool hasCollider = false)
        {
            var massProperties = inMassProperties;
            var scale = 1f;

            // Scale the provided mass properties by the LocalTransform.Scale value to create the correct
            // initial mass distribution for the rigid body.
            if (entityManager.HasComponent<LocalTransform>(entity))
            {
                var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
                scale = localTransform.Scale;

                massProperties.Scale(scale);
            }

            // Override the mass properties with user-provided values if specified
            if (inBodyData.OverrideDefaultMassDistribution)
            {
                massProperties.MassDistribution = inBodyData.CustomMassDistribution;
                if (hasCollider)
                {
                    // Increase the angular expansion factor to account for the shift in center of mass
                    massProperties.AngularExpansionFactor += math.length(massProperties.MassDistribution.Transform.pos -
                        inBodyData.CustomMassDistribution.Transform.pos);
                }
            }

            // Create the physics mass properties. Among others, this scales the unit mass inertia tensor
            // by the scalar mass of the rigid body.
            var physicsMass = inBodyData.IsDynamic ?
                PhysicsMass.CreateDynamic(massProperties, inBodyData.Mass) :
                PhysicsMass.CreateKinematic(massProperties);

            // Now, apply inverse scale to the final, baked physics mass properties in order to prevent invalid simulated mass properties
            // caused by runtime scaling of the mass properties later on while building the physics world.
            physicsMass = physicsMass.ApplyScale(math.rcp(scale));

            return physicsMass;
        }
    }
}
