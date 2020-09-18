using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Joint = Unity.Physics.Joint;
using Material = UnityEngine.Material;

public struct CustomVelocity : IComponentData
{
    public float3 Linear;
    public float3 Angular;
}

public struct CustomCollider : IComponentData
{
    public BlobAssetReference<Collider> ColliderRef;
}

public class SingleThreadedPhysics : MonoBehaviour
{
    public Material ReferenceMaterial;

    private void Start()
    {
        var system = BasePhysicsDemo.DefaultWorld.GetExistingSystem<SingleThreadedPhysicsSystem>();
        system.Initialize(ReferenceMaterial);
    }

    private void OnEnable()
    {
        var system = BasePhysicsDemo.DefaultWorld.GetExistingSystem<SingleThreadedPhysicsSystem>();
        system.Enabled = true;
    }

    private void OnDestroy()
    {
        var system = BasePhysicsDemo.DefaultWorld.GetExistingSystem<SingleThreadedPhysicsSystem>();
        system.Enabled = false;
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(StepPhysicsWorld))]
public class SingleThreadedPhysicsSystem : SystemBase
{
    public PhysicsWorld PhysicsWorld = new PhysicsWorld(0, 0, 0);

    public EntityQuery CustomDynamicEntityGroup;
    public EntityQuery CustomStaticEntityGroup;
    public EntityQuery JointEntityGroup;

    private NativeHashMap<Entity, Entity> EntityMap;
    private NativeHashMap<Entity, int> EntityToBodyIndexMap;

    private StepPhysicsWorld m_StepPhysicsWorld;

    private SimulationContext SimulationContext;
#if HAVOK_PHYSICS_EXISTS
    private Havok.Physics.SimulationContext HavokSimulationContext;
#endif

    // Static and dynamic rigid bodies
    public unsafe void CreateRigidBodies()
    {
        NativeArray<RigidBody> dynamicBodies = PhysicsWorld.DynamicBodies;
        NativeArray<RigidBody> staticBodies = PhysicsWorld.StaticBodies;

        // Creating dynamic bodies
        {
            NativeArray<CustomCollider> colliders = CustomDynamicEntityGroup.ToComponentDataArray<CustomCollider>(Allocator.TempJob);
            NativeArray<Translation> positions = CustomDynamicEntityGroup.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Rotation> rotations = CustomDynamicEntityGroup.ToComponentDataArray<Rotation>(Allocator.TempJob);
            NativeArray<PhysicsCustomTags> customTags = CustomDynamicEntityGroup.ToComponentDataArray<PhysicsCustomTags>(Allocator.TempJob);
            NativeArray<Entity> entities = CustomDynamicEntityGroup.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                dynamicBodies[i] = new RigidBody
                {
                    WorldFromBody = new RigidTransform(rotations[i].Value, positions[i].Value),
                    Collider = colliders[i].ColliderRef,
                    Entity = entities[i],
                    CustomTags = customTags[i].Value
                };
                EntityToBodyIndexMap.Add(entities[i], i);
            }

            colliders.Dispose();
            positions.Dispose();
            rotations.Dispose();
            customTags.Dispose();
            entities.Dispose();
        }

        // Creating static bodies
        {
            NativeArray<CustomCollider> colliders = CustomStaticEntityGroup.ToComponentDataArray<CustomCollider>(Allocator.TempJob);
            NativeArray<Translation> positions = CustomStaticEntityGroup.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Rotation> rotations = CustomStaticEntityGroup.ToComponentDataArray<Rotation>(Allocator.TempJob);
            NativeArray<PhysicsCustomTags> customTags = CustomStaticEntityGroup.ToComponentDataArray<PhysicsCustomTags>(Allocator.TempJob);
            NativeArray<Entity> entities = CustomStaticEntityGroup.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                staticBodies[i] = new RigidBody
                {
                    WorldFromBody = new RigidTransform(rotations[i].Value, positions[i].Value),
                    Collider = colliders[i].ColliderRef,
                    Entity = entities[i],
                    CustomTags = customTags[i].Value
                };
                EntityToBodyIndexMap.Add(entities[i], i + dynamicBodies.Length);
            }

            // default static body
            staticBodies[entities.Length] = new RigidBody
            {
                WorldFromBody = new RigidTransform(quaternion.identity, float3.zero),
                Collider = default,
                Entity = Entity.Null,
                CustomTags = 0
            };

            colliders.Dispose();
            positions.Dispose();
            rotations.Dispose();
            customTags.Dispose();
            entities.Dispose();
        }
    }

    public void CreateMotionVelocities()
    {
        NativeArray<CustomVelocity> customVelocities = CustomDynamicEntityGroup.ToComponentDataArray<CustomVelocity>(Allocator.TempJob);
        NativeArray<PhysicsMass> masses = CustomDynamicEntityGroup.ToComponentDataArray<PhysicsMass>(Allocator.TempJob);
        NativeArray<PhysicsGravityFactor> gravityFactors = CustomDynamicEntityGroup.ToComponentDataArray<PhysicsGravityFactor>(Allocator.TempJob);
        NativeArray<MotionVelocity> motionVelocities = PhysicsWorld.MotionVelocities;

        for (int i = 0; i < customVelocities.Length; i++)
        {
            motionVelocities[i] = new MotionVelocity
            {
                LinearVelocity = customVelocities[i].Linear,
                AngularVelocity = customVelocities[i].Angular,
                InverseInertia = masses[i].InverseInertia,
                InverseMass = masses[i].InverseMass,
                AngularExpansionFactor = masses[i].AngularExpansionFactor,
                GravityFactor = gravityFactors[i].Value
            };
        }

        customVelocities.Dispose();
        masses.Dispose();
        gravityFactors.Dispose();
    }

    public void CreateMotionDatas()
    {
        NativeArray<Translation> positions = CustomDynamicEntityGroup.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Rotation> rotations = CustomDynamicEntityGroup.ToComponentDataArray<Rotation>(Allocator.TempJob);
        NativeArray<PhysicsMass> masses = CustomDynamicEntityGroup.ToComponentDataArray<PhysicsMass>(Allocator.TempJob);
        NativeArray<PhysicsDamping> dampings = CustomDynamicEntityGroup.ToComponentDataArray<PhysicsDamping>(Allocator.TempJob);

        NativeArray<MotionData> motionDatas = PhysicsWorld.MotionDatas;
        for (int i = 0; i < positions.Length; i++)
        {
            motionDatas[i] = new MotionData
            {
                WorldFromMotion = new RigidTransform(
                    math.mul(rotations[i].Value, masses[i].InertiaOrientation),
                    math.rotate(rotations[i].Value, masses[i].CenterOfMass) + positions[i].Value
                    ),
                BodyFromMotion = new RigidTransform(masses[i].InertiaOrientation, masses[i].CenterOfMass),
                LinearDamping = dampings[i].Linear,
                AngularDamping = dampings[i].Angular
            };
        }

        positions.Dispose();
        rotations.Dispose();
        masses.Dispose();
        dampings.Dispose();
    }

    public void CreateJoints()
    {
        NativeArray<PhysicsConstrainedBodyPair> constrainedBodyPairs = JointEntityGroup.ToComponentDataArray<PhysicsConstrainedBodyPair>(Allocator.TempJob);
        NativeArray<PhysicsJoint> physicsJoints = JointEntityGroup.ToComponentDataArray<PhysicsJoint>(Allocator.TempJob);
        NativeArray<Joint> joints = PhysicsWorld.Joints;

        for (int i = 0; i < physicsJoints.Length; i++)
        {
            EntityMap.TryGetValue(constrainedBodyPairs[i].EntityA, out Entity entityA);
            EntityMap.TryGetValue(constrainedBodyPairs[i].EntityB, out Entity entityB);
            int bodyIndexA = -1;
            if (entityA != Entity.Null)
            {
                EntityToBodyIndexMap.TryGetValue(entityA, out bodyIndexA);
            }
            else
            {
                bodyIndexA = PhysicsWorld.NumBodies - 1;
            }
            int bodyIndexB = -1;
            if (entityB != Entity.Null)
            {
                EntityToBodyIndexMap.TryGetValue(entityB, out bodyIndexB);
            }
            else
            {
                bodyIndexB = PhysicsWorld.NumBodies - 1;
            }

            var pair = new BodyIndexPair
            {
                BodyIndexA = bodyIndexA,
                BodyIndexB = bodyIndexB
            };
            var jointData = physicsJoints[i];
            joints[i] = new Joint
            {
                BodyPair = pair,
                Entity = Entity.Null,
                AFromJoint = new Math.MTransform(jointData.BodyAFromJoint.AsRigidTransform()),
                BFromJoint = new Math.MTransform(jointData.BodyBFromJoint.AsRigidTransform()),
                EnableCollision = (byte)constrainedBodyPairs[i].EnableCollision,
                Version = jointData.Version,
                Constraints = jointData.GetConstraints()
            };
        }

        physicsJoints.Dispose();
        constrainedBodyPairs.Dispose();
    }

    public void ExportMotions(NativeArray<RigidBody> dynamicBodies, NativeArray<MotionData> motionDatas, NativeArray<MotionVelocity> motionVelocities)
    {
        for (int i = 0; i < dynamicBodies.Length; i++)
        {
            MotionData md = motionDatas[i];
            RigidTransform worldFromBody = math.mul(md.WorldFromMotion, math.inverse(md.BodyFromMotion));

            EntityManager.SetComponentData(dynamicBodies[i].Entity, new Translation { Value = worldFromBody.pos });
            EntityManager.SetComponentData(dynamicBodies[i].Entity, new Rotation { Value = worldFromBody.rot});
            EntityManager.SetComponentData(dynamicBodies[i].Entity, new CustomVelocity { Linear = motionVelocities[i].LinearVelocity, Angular = motionVelocities[i].AngularVelocity});
        }
    }

    // Custom data initialization
    public void Initialize(Material referenceMaterial)
    {
        // Key is new entity that gets copied from old entity
        EntityQuery query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(Translation),
                typeof(Rotation),
            }
        });

        var entities = query.ToEntityArray(Allocator.TempJob);
        if (EntityMap.IsCreated)
            EntityMap.Dispose();

        EntityMap = new NativeHashMap<Entity, Entity>(entities.Length, Allocator.Persistent);

        for (int i = 0; i < entities.Length; i++)
        {
            if (!EntityManager.HasComponent(entities[i], typeof(RenderMesh)))
            {
                continue;
            }
            var ghostMaterial = new RenderMesh
            {
                mesh = EntityManager.GetSharedComponentData<RenderMesh>(entities[i]).mesh,
                material = referenceMaterial
            };

            var ghost = EntityManager.Instantiate(entities[i]);

            EntityMap.Add(entities[i], ghost);

            var defaultPhysicsMass = new PhysicsMass()
            {
                Transform = RigidTransform.identity,
                InverseMass = 0.0f,
                InverseInertia = float3.zero,
                AngularExpansionFactor = 1.0f,
            };

            var defaultPhysicsDamping = new PhysicsDamping()
            {
                Linear = 0.0f,
                Angular = 0.0f,
            };

            // default gravity factor
            var defaultGravityFactor = new PhysicsGravityFactor
            {
                Value = 1.0f
            };

            var defaultPhysicsTags = new PhysicsCustomTags { Value = 0 };

            if (!EntityManager.HasComponent<PhysicsCustomTags>(entities[i]))
            {
                EntityManager.AddComponentData(ghost, defaultPhysicsTags);
            }

            if (!EntityManager.HasComponent<PhysicsMass>(entities[i]))
            {
                defaultGravityFactor.Value = 0f;
                EntityManager.AddComponentData(ghost, defaultPhysicsMass);
            }

            if (!EntityManager.HasComponent<PhysicsDamping>(entities[i]))
            {
                EntityManager.AddComponentData(ghost, defaultPhysicsDamping);
            }

            if (!EntityManager.HasComponent<PhysicsGravityFactor>(entities[i]))
            {
                EntityManager.AddComponentData(ghost, defaultGravityFactor);
            }

            if (!EntityManager.HasComponent<CustomCollider>(entities[i]))
            {
                CustomCollider customCollider;
                if (EntityManager.HasComponent<PhysicsCollider>(entities[i]))
                {
                    unsafe
                    {
                        customCollider.ColliderRef = BlobAssetReference<Collider>.Create(
                            EntityManager.GetComponentData<PhysicsCollider>(entities[i]).ColliderPtr,
                            EntityManager.GetComponentData<PhysicsCollider>(entities[i]).ColliderPtr->MemorySize);
                    }
                }
                else
                {
                    customCollider.ColliderRef = default;
                }

                EntityManager.AddComponentData(ghost, customCollider);
                EntityManager.RemoveComponent<PhysicsCollider>(ghost);
            }

            if (EntityManager.HasComponent<PhysicsVelocity>(entities[i]))
            {
                var physVel = EntityManager.GetComponentData<PhysicsVelocity>(entities[i]);
                var customVel = new CustomVelocity { Linear = physVel.Linear, Angular = physVel.Angular };
                EntityManager.AddComponentData(ghost, customVel);
            }

            var position = EntityManager.GetComponentData<Translation>(entities[i]);
            // The idea is that static bodies overlap, and dynamic ones are separated from original ones
            position.Value = new float3(position.Value.x, position.Value.y, position.Value.z);

            EntityManager.SetComponentData(ghost, position);
            EntityManager.RemoveComponent<PhysicsVelocity>(ghost);

            EntityManager.SetSharedComponentData(ghost, ghostMaterial);
        }

        entities.Dispose();

        SimulationContext = new SimulationContext();
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext = new Havok.Physics.SimulationContext(Havok.Physics.HavokConfiguration.Default);
#endif
    }

    protected override void OnCreate()
    {
        Enabled = false;
        CustomDynamicEntityGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CustomVelocity),
                typeof(Translation),
                typeof(Rotation),
                typeof(CustomCollider),
                typeof(PhysicsCustomTags),
                typeof(PhysicsMass),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor)
            },
            None = new ComponentType[]
            {
                typeof(PhysicsExclude)
            }
        });

        CustomStaticEntityGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CustomCollider),
                typeof(Translation),
                typeof(Rotation),
                typeof(PhysicsCustomTags)
            },
            None = new ComponentType[]
            {
                typeof(CustomVelocity),
                typeof(PhysicsExclude)
            }
        });

        JointEntityGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(PhysicsConstrainedBodyPair),
                typeof(PhysicsJoint)
            },
            None = new ComponentType[]
            {
                typeof(PhysicsExclude)
            }
        });

        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        EntityMap = new NativeHashMap<Entity, Entity>(0, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        // Make sure regular physics world is stepped
        m_StepPhysicsWorld.GetOutputDependency().Complete();

        int numDynamicBodies = CustomDynamicEntityGroup.CalculateEntityCount();
        int numStaticBodies = CustomStaticEntityGroup.CalculateEntityCount();
        int numJoints = JointEntityGroup.CalculateEntityCount();

        EntityToBodyIndexMap = new NativeHashMap<Entity, int>(numStaticBodies + numDynamicBodies, Allocator.Temp);
        numStaticBodies++; // + 1 for default static body

        // Build the world
        {
            PhysicsWorld.Reset(numStaticBodies, numDynamicBodies, numJoints);

            CreateRigidBodies();
            CreateMotionVelocities();
            CreateMotionDatas();
            CreateJoints();
        }

        // Step the world
        if (PhysicsWorld.NumDynamicBodies != 0)
        {
            PhysicsStep stepComponent = PhysicsStep.Default;
            if (HasSingleton<PhysicsStep>())
            {
                stepComponent = GetSingleton<PhysicsStep>();
            }

            SimulationStepInput input = new SimulationStepInput
            {
                World = PhysicsWorld,
                TimeStep = Time.DeltaTime,
                SolverStabilizationHeuristicSettings = stepComponent.SolverStabilizationHeuristicSettings,
                NumSolverIterations = stepComponent.SolverIterationCount,
                Gravity = stepComponent.Gravity
            };

            if (stepComponent.SimulationType == SimulationType.UnityPhysics)
            {
                SimulationContext.Reset(input);

                new SimulateSingleThreadedJob
                {
                    Input = input,
                    SimulationContext = SimulationContext
                }.Schedule().Complete();
            }
#if HAVOK_PHYSICS_EXISTS
            else
            {
                HavokSimulationContext.Reset(ref PhysicsWorld);

                new SimulateSingleThreadedHavokJob
                {
                    Input = input,
                    SimulationContext = HavokSimulationContext
                }.Schedule().Complete();
            }
#endif
        }

        // Export the data
        ExportMotions(PhysicsWorld.DynamicBodies, PhysicsWorld.MotionDatas, PhysicsWorld.MotionVelocities);
    }

    [BurstCompile]
    public struct SimulateSingleThreadedJob : IJob
    {
        public SimulationStepInput Input;
        public SimulationContext SimulationContext;

        public void Execute()
        {
            // Build broad phase
            Input.World.CollisionWorld.BuildBroadphase(ref Input.World, Input.TimeStep, Input.Gravity);

            // Step the simulation
            Simulation.StepImmediate(Input, ref SimulationContext);
        }
    }

#if HAVOK_PHYSICS_EXISTS
    [BurstCompile]
    public struct SimulateSingleThreadedHavokJob : IJob
    {
        public SimulationStepInput Input;
        public Havok.Physics.SimulationContext SimulationContext;

        public void Execute()
        {
            // Build broad phase
            Input.World.CollisionWorld.BuildBroadphase(ref Input.World, Input.TimeStep, Input.Gravity);

            // Step the simulation
            Havok.Physics.HavokSimulation.StepImmediate(Input, ref SimulationContext);
        }
    }
#endif

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EntityMap.IsCreated)
        {
            EntityMap.Dispose();
        }

        PhysicsWorld.Dispose();
        SimulationContext.Dispose();
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext.Dispose();
#endif
    }
}
