using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Assertions;
using static RagdollDemoUtilities;
using Collider = Unity.Physics.Collider;

public class SingleThreadedRagdoll : MonoBehaviour
{
    public PhysicsWorld PhysicsWorld;

    private SimulationContext SimulationContext;
#if HAVOK_PHYSICS_EXISTS
    private Havok.Physics.SimulationContext HavokSimulationContext;
    protected bool SimulateHavok = false; // set based on the PhysicsStep component.
#endif

    public bool DrawDebugInformation = false;

    private List<BlobAssetReference<Collider>> m_CreatedColliders = null;

    static readonly System.Type k_DrawComponent = typeof(Unity.Physics.Authoring.DisplayBodyColliders)
        .GetNestedType("DrawComponent", BindingFlags.NonPublic);
    static readonly MethodInfo k_DrawComponent_DrawColliderEdges = k_DrawComponent
        .GetMethod("DrawColliderEdges", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(BlobAssetReference<Collider>), typeof(RigidTransform), typeof(bool) }, null);

    public void OnDrawGizmos()
    {
        if (!DrawDebugInformation || !m_BodyInfos.IsCreated || !m_JointInfos.IsCreated) return;

        // Debug draw the colliders
        for (int i = 0; i < m_BodyInfos.Length; i++)
        {
            m_BodyInfoIndexToGameObjectMapping.TryGetValue(i, out GameObject g);

            var collider = m_BodyInfos[i].Collider;
            var transform = new RigidTransform(m_BodyInfos[i].Orientation, m_BodyInfos[i].Position);

            k_DrawComponent_DrawColliderEdges.Invoke(null, new object[] { collider, transform, false });
        }

        // Debug draw the joints
        {
            Color originalColor = Gizmos.color;

            for (int i = 0; i < m_JointInfos.Length; i++)
            {
                var jointInfo = m_JointInfos[i];
                var transformBodyA = new RigidTransform(m_BodyInfos[jointInfo.BodyIndexA].Orientation, m_BodyInfos[jointInfo.BodyIndexA].Position);
                var transformBodyB = new RigidTransform(m_BodyInfos[jointInfo.BodyIndexB].Orientation, m_BodyInfos[jointInfo.BodyIndexB].Position);

                transformBodyA = math.mul(transformBodyA, jointInfo.JointData.BodyAFromJoint.AsRigidTransform());
                transformBodyB = math.mul(transformBodyB, jointInfo.JointData.BodyBFromJoint.AsRigidTransform());

                Vector3 pivotAWorld = math.transform(transformBodyA, float3.zero);
                Vector3 pivotBWorld = math.transform(transformBodyB, float3.zero);

                var colorA = Color.cyan;
                var colorB = Color.magenta;
                var size = 0.25f;
                for (int j = 0; j < 3; j++)
                {
                    var from = Vector3.zero; from[j] = -size;
                    var to = -from;
                    Gizmos.color = colorA;
                    Gizmos.DrawLine(from + pivotAWorld, to + pivotAWorld);
                    Gizmos.color = colorB;
                    Gizmos.DrawLine(from + pivotBWorld, to + pivotBWorld);
                }
            }
            Gizmos.color = originalColor;
        }
    }

    public void Update()
    {
        // +1 default static body
        var NumStaticBodies = (m_BodyInfos.Length - m_NumDynamicBodies) + 1;

        PhysicsWorld.Reset(NumStaticBodies, m_NumDynamicBodies, m_JointInfos.Length);

        SimulationStepInput input = new SimulationStepInput
        {
            World = PhysicsWorld,
            TimeStep = World.DefaultGameObjectInjectionWorld.GetExistingSystem<FixedStepSimulationSystemGroup>().Timestep,
            NumSolverIterations = PhysicsStep.Default.SolverIterationCount,
            SolverStabilizationHeuristicSettings = PhysicsStep.Default.SolverStabilizationHeuristicSettings,
            Gravity = PhysicsStep.Default.Gravity
        };

        int numOfBodies = m_NumDynamicBodies + NumStaticBodies;

        using (var indexMap = new NativeHashMap<int, int>(m_BodyInfos.Length, Allocator.TempJob))
        {
#if HAVOK_PHYSICS_EXISTS
            if (SimulateHavok)
            {
                HavokSimulationContext.Reset(ref PhysicsWorld);

                new SingleThreadedPhysicsHavokSimulationJob
                {
                    Bodies = m_BodyInfos,
                    Joints = m_JointInfos,
                    Input = input,
                    SimulationContext = HavokSimulationContext,
                    BodyInfoToBodiesIndexMap = indexMap
                }.Run();
            }
            else
#endif
            {
                SimulationContext.Reset(input);

                new SingleThreadedPhysicsSimulationJob
                {
                    BodyInfos = m_BodyInfos,
                    JointInfos = m_JointInfos,
                    Input = input,
                    SimulationContext = SimulationContext,
                    BodyInfoToBodiesIndexMap = indexMap
                }.Run();
            }
        }

        // Map the results to GameObjects
        for (int i = 0; i < m_BodyInfos.Length; i++)
        {
            if (!m_BodyInfos[i].IsDynamic) continue;

            m_BodyInfoIndexToGameObjectMapping.TryGetValue(i, out GameObject g);
            g.transform.position = m_BodyInfos[i].Position;
            g.transform.rotation = m_BodyInfos[i].Orientation;
        }
    }

    private int m_NumDynamicBodies = 0;

    private NativeList<BodyInfo> m_BodyInfos;
    private NativeList<JointInfo> m_JointInfos;
    private Dictionary<int, GameObject> m_BodyInfoIndexToGameObjectMapping;

    [BurstCompile]
    private struct SingleThreadedPhysicsSimulationJob : IJob
    {
        public SimulationStepInput Input;
        public SimulationContext SimulationContext;

        public NativeList<BodyInfo> BodyInfos;
        public NativeList<JointInfo> JointInfos;

        public NativeHashMap<int, int> BodyInfoToBodiesIndexMap;

        internal static void CreateBodies(SimulationStepInput input,
            NativeList<BodyInfo> bodyInfos, NativeHashMap<int, int> bodyInfoToBodiesIndexMap)
        {
            NativeArray<RigidBody> dynamicBodies = input.World.DynamicBodies;
            NativeArray<RigidBody> staticBodies = input.World.StaticBodies;
            NativeArray<MotionData> motionDatas = input.World.MotionDatas;
            NativeArray<MotionVelocity> motionVelocities = input.World.MotionVelocities;

            int dynamicBodyIndex = 0;
            int staticBodyIndex = 0;

            for (int i = 0; i < bodyInfos.Length; i++)
            {
                BodyInfo bodyInfo = bodyInfos[i];
                var collider = bodyInfo.Collider;
                if (bodyInfo.IsDynamic)
                {
                    dynamicBodies[dynamicBodyIndex] = new RigidBody
                    {
                        WorldFromBody = new RigidTransform(bodyInfo.Orientation, bodyInfo.Position),
                        Collider = bodyInfo.Collider,
                        Entity = Entity.Null,
                        CustomTags = 0
                    };
                    motionDatas[dynamicBodyIndex] = new MotionData
                    {
                        WorldFromMotion = new RigidTransform(
                            math.mul(bodyInfo.Orientation, collider.Value.MassProperties.MassDistribution.Transform.rot),
                            math.rotate(bodyInfo.Orientation, collider.Value.MassProperties.MassDistribution.Transform.pos) + bodyInfo.Position
                            ),
                        BodyFromMotion = new RigidTransform(collider.Value.MassProperties.MassDistribution.Transform.rot, collider.Value.MassProperties.MassDistribution.Transform.pos),
                        LinearDamping = 0.0f,
                        AngularDamping = 0.0f
                    };
                    motionVelocities[dynamicBodyIndex] = new MotionVelocity
                    {
                        LinearVelocity = bodyInfo.LinearVelocity,
                        AngularVelocity = bodyInfo.AngularVelocity,
                        InverseInertia = math.rcp(collider.Value.MassProperties.MassDistribution.InertiaTensor * bodyInfo.Mass),
                        InverseMass = math.rcp(bodyInfo.Mass),
                        AngularExpansionFactor = collider.Value.MassProperties.AngularExpansionFactor,
                        GravityFactor = 1.0f
                    };
                    bodyInfoToBodiesIndexMap.Add(i, dynamicBodyIndex);
                    dynamicBodyIndex++;
                }
                else
                {
                    staticBodies[staticBodyIndex] = new RigidBody
                    {
                        WorldFromBody = new RigidTransform(bodyInfo.Orientation, bodyInfo.Position),
                        Collider = bodyInfo.Collider,
                        Entity = Entity.Null,
                        CustomTags = 0
                    };
                    staticBodyIndex++;
                    bodyInfoToBodiesIndexMap.Add(i, -staticBodyIndex);
                }
            }
            for (int i = 0; i < bodyInfos.Length; i++)
            {
                if (0 > bodyInfoToBodiesIndexMap[i])
                {
                    bodyInfoToBodiesIndexMap[i]++;
                    bodyInfoToBodiesIndexMap[i] = -bodyInfoToBodiesIndexMap[i];
                    bodyInfoToBodiesIndexMap[i] += dynamicBodyIndex;
                }
            }

            // Create default static body
            staticBodies[staticBodyIndex] = new RigidBody
            {
                WorldFromBody = new RigidTransform(quaternion.identity, float3.zero),
                Collider = default,
                Entity = Entity.Null,
                CustomTags = 0
            };
        }

        internal static void CreateJoints(SimulationStepInput input,
            NativeList<JointInfo> jointInfos, NativeHashMap<int, int> bodyInfoToBodiesIndexMap)
        {
            NativeArray<Unity.Physics.Joint> joints = input.World.Joints;

            for (int i = 0; i < jointInfos.Length; i++)
            {
                var jointInfo = jointInfos[i];

                bodyInfoToBodiesIndexMap.TryGetValue(jointInfo.BodyIndexA, out int bodyIndexA);
                bodyInfoToBodiesIndexMap.TryGetValue(jointInfo.BodyIndexB, out int bodyIndexB);
                BodyIndexPair pair = new BodyIndexPair
                {
                    BodyIndexA = bodyIndexA,
                    BodyIndexB = bodyIndexB,
                };

                joints[i] = new Unity.Physics.Joint
                {
                    BodyPair = pair,
                    Entity = Entity.Null,
                    AFromJoint = new Math.MTransform(jointInfo.JointData.BodyAFromJoint.AsRigidTransform()),
                    BFromJoint = new Math.MTransform(jointInfo.JointData.BodyBFromJoint.AsRigidTransform()),
                    EnableCollision = (byte)(jointInfo.EnableCollision ? 1 : 0),
                    Version = jointInfo.JointData.Version,
                    Constraints = jointInfo.JointData.GetConstraints()
                };
            }
        }

        internal static void ExportData(SimulationStepInput input, NativeList<BodyInfo> bodyInfos)
        {
            int dynamicBodyIndex = 0;
            for (int i = 0; i < bodyInfos.Length; i++)
            {
                if (!bodyInfos[i].IsDynamic) continue;

                MotionData md = input.World.MotionDatas[dynamicBodyIndex];
                RigidTransform worldFromBody = math.mul(md.WorldFromMotion, math.inverse(md.BodyFromMotion));

                BodyInfo bodyInfo = bodyInfos[i];
                bodyInfo.Position = worldFromBody.pos;
                bodyInfo.Orientation = worldFromBody.rot;
                bodyInfo.LinearVelocity = input.World.MotionVelocities[dynamicBodyIndex].LinearVelocity;
                bodyInfo.AngularVelocity = input.World.MotionVelocities[dynamicBodyIndex].AngularVelocity;
                bodyInfos[i] = bodyInfo;

                dynamicBodyIndex++;
            }
        }

        public void Execute()
        {
            // Create the physics world
            CreateBodies(Input, BodyInfos, BodyInfoToBodiesIndexMap);
            CreateJoints(Input, JointInfos, BodyInfoToBodiesIndexMap);

            // Build the broadphase
            Input.World.CollisionWorld.BuildBroadphase(ref Input.World, Input.TimeStep, Input.Gravity);

            // Step the physics world
            Simulation.StepImmediate(Input, ref SimulationContext);

            // Export the changed motion data to body info
            ExportData(Input, BodyInfos);
        }
    }

#if HAVOK_PHYSICS_EXISTS
    private struct SingleThreadedPhysicsHavokSimulationJob : IJob
    {
        public SimulationStepInput Input;
        public Havok.Physics.SimulationContext SimulationContext;

        public NativeList<BodyInfo> Bodies;
        public NativeList<JointInfo> Joints;

        public NativeHashMap<int, int> BodyInfoToBodiesIndexMap;

        public void Execute()
        {
            // Create the physics world
            SingleThreadedPhysicsSimulationJob.CreateBodies(Input, Bodies, BodyInfoToBodiesIndexMap);
            SingleThreadedPhysicsSimulationJob.CreateJoints(Input, Joints, BodyInfoToBodiesIndexMap);

            // Build the broadphase
            Input.World.CollisionWorld.BuildBroadphase(ref Input.World, Input.TimeStep, Input.Gravity);

            // Step the physics world
            Havok.Physics.HavokSimulation.StepImmediate(Input, ref SimulationContext);

            // Export the changed motion data to body info
            SingleThreadedPhysicsSimulationJob.ExportData(Input, Bodies);
        }
    }
#endif

    public void Start()
    {
        SimulationContext = new SimulationContext();
        m_CreatedColliders = new List<BlobAssetReference<Collider>>();

#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext = new Havok.Physics.SimulationContext(Havok.Physics.HavokConfiguration.Default);

        PhysicsStep stepComponent = PhysicsStep.Default;
        var buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        if (buildPhysicsWorld.HasSingleton<PhysicsStep>())
        {
            stepComponent = buildPhysicsWorld.GetSingleton<PhysicsStep>();
            SimulateHavok = (stepComponent.SimulationType == SimulationType.HavokPhysics);
        }
#endif

        m_BodyInfoIndexToGameObjectMapping = new Dictionary<int, GameObject>();
        m_BodyInfos = new NativeList<BodyInfo>(Allocator.Persistent);
        m_JointInfos = new NativeList<JointInfo>(Allocator.Persistent);
        PhysicsWorld = new PhysicsWorld(0, 0, 0);

        // Create all the Bodies
        var basicBodyInfos = GameObject.FindObjectsOfType<BasicBodyInfo>();
        for (int i = 0; i < basicBodyInfos.Length; i++)
        {
            var basicBodyInfo = basicBodyInfos[i];
            var body = CreateBody(basicBodyInfo.gameObject, m_CreatedColliders);
            if (body.IsDynamic)
            {
                m_NumDynamicBodies++;
            }
            m_BodyInfoIndexToGameObjectMapping.Add(i, basicBodyInfo.gameObject);
            m_BodyInfos.Add(body);
        }

        // Create all the Joints
        var basicJointInfos = GameObject.FindObjectsOfType<BasicJointInfo>();
        for (int i = 0; i < basicJointInfos.Length; i++)
        {
            var basicJointInfo = basicJointInfos[i];

            Assert.IsTrue(basicJointInfo.ConnectedGameObject != null);

            GameObject bodyA = basicJointInfo.ConnectedGameObject;
            GameObject bodyB = basicJointInfo.gameObject;

            var jointData = CreateJoint(bodyA, bodyB, basicJointInfo.Type);
            GetBodyIndices(bodyA, bodyB, out int bodyIndexA, out int bodyIndexB);
            var joint = new JointInfo
            {
                JointData = jointData,
                BodyIndexA = bodyIndexA,
                BodyIndexB = bodyIndexB,
                EnableCollision = false,
            };
            m_JointInfos.Add(joint);
        }
    }

    private void GetBodyIndices(GameObject bodyA, GameObject bodyB, out int bodyIndexA, out int bodyIndexB)
    {
        bodyIndexA = bodyIndexB = -1;

        for (int i = 0; i < m_BodyInfos.Length; i++)
        {
            if (m_BodyInfoIndexToGameObjectMapping.TryGetValue(i, out GameObject go))
            {
                if (go == bodyA)
                {
                    bodyIndexA = i;
                }

                if (go == bodyB)
                {
                    bodyIndexB = i;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (m_BodyInfos.IsCreated)
        {
            m_BodyInfos.Dispose();
        }

        if (m_JointInfos.IsCreated)
        {
            m_JointInfos.Dispose();
        }

        for (int i = 0; i < m_CreatedColliders.Count; i++)
        {
            m_CreatedColliders[i].Dispose();
        }

        PhysicsWorld.Dispose();
        SimulationContext.Dispose();
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext.Dispose();
#endif
    }
}
