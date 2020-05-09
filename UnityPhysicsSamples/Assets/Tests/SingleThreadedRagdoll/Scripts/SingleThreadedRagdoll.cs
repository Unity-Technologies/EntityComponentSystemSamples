using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Assertions;
using static RagdollDemoUtilities;

public class SingleThreadedRagdoll : MonoBehaviour
{
    public PhysicsWorld PhysicsWorld;

    private SimulationContext SimulationContext;
#if HAVOK_PHYSICS_EXISTS
    private Havok.Physics.SimulationContext HavokSimulationContext;
    public bool SimulateHavok = false;
#endif

    public void Update()
    {
        // +1 for the default static body
        PhysicsWorld.Reset(m_NumStaticBodies + 1, m_NumDynamicBodies, m_NumJoints);

        SimulationStepInput input = new SimulationStepInput
        {
            World = PhysicsWorld,
            TimeStep = Time.fixedDeltaTime,
            NumSolverIterations = PhysicsStep.Default.SolverIterationCount,
            Gravity = PhysicsStep.Default.Gravity
        };

        int numOfBodies = m_NumDynamicBodies + m_NumStaticBodies;

        NativeHashMap<int, int> indexMap = new NativeHashMap<int, int>(m_Bodies.Length, Allocator.TempJob);

#if HAVOK_PHYSICS_EXISTS
        if (SimulateHavok)
        {
            HavokSimulationContext.Reset(ref PhysicsWorld);

            new SingleThreadedPhysicsHavokSimulationJob
            {
                Bodies = m_Bodies,
                Joints = m_Joints,
                Input = input,
                SimulationContext = HavokSimulationContext,
                IndexMap = indexMap
            }.Schedule().Complete();
        }
        else
#endif
        {
            SimulationContext.Reset(ref PhysicsWorld);

            new SingleThreadedPhysicsSimulationJob
            {
                Bodies = m_Bodies,
                Joints = m_Joints,
                Input = input,
                SimulationContext = SimulationContext,
                IndexMap = indexMap
            }.Schedule().Complete();
        }

        // Map the results to GameObjects
        for (int i = 0; i < numOfBodies; i++)
        {
            if (!m_Bodies[i].IsDynamic)
            {
                continue;
            }

            m_BodyIndexToGameObjectMapping.TryGetValue(i, out GameObject g);
            var t = g.GetComponent<Transform>();
            t.position = m_Bodies[i].Position;
            t.rotation = m_Bodies[i].Orientation;
        }

        indexMap.Dispose();
    }

    private int m_NumStaticBodies = 0;
    private int m_NumDynamicBodies = 0;
    private int m_NumJoints = 0;

    private NativeList<BodyInfo> m_Bodies;
    private NativeList<JointInfo> m_Joints;
    private Dictionary<int, GameObject> m_BodyIndexToGameObjectMapping;

    [BurstCompile]
    private struct SingleThreadedPhysicsSimulationJob : IJob
    {
        public SimulationStepInput Input;
        public SimulationContext SimulationContext;

        public NativeList<BodyInfo> Bodies;
        public NativeList<JointInfo> Joints;

        public NativeHashMap<int, int> IndexMap;

        internal static void CreateRigidBodiesAndMotions(SimulationStepInput input,
            NativeList<BodyInfo> bodies, NativeHashMap<int, int> indexMap)
        {
            NativeSlice<RigidBody> dynamicBodies = input.World.DynamicBodies;
            NativeSlice<RigidBody> staticBodies = input.World.StaticBodies;
            NativeSlice<MotionData> motionDatas = input.World.MotionDatas;
            NativeSlice<MotionVelocity> motionVelocities = input.World.MotionVelocities;

            int dynamicBodyIndex = 0;
            int staticBodyIndex = 0;

            for (int i = 0; i < bodies.Length; i++)
            {
                BodyInfo bodyInfo = bodies[i];

                unsafe
                {
                    Unity.Physics.Collider* collider = (Unity.Physics.Collider*)bodyInfo.Collider.GetUnsafePtr();

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
                                math.mul(bodyInfo.Orientation, collider->MassProperties.MassDistribution.Transform.rot),
                                math.rotate(bodyInfo.Orientation, collider->MassProperties.MassDistribution.Transform.pos) + bodyInfo.Position
                            ),
                            BodyFromMotion = new RigidTransform(collider->MassProperties.MassDistribution.Transform.rot, collider->MassProperties.MassDistribution.Transform.pos),
                            LinearDamping = 0.0f,
                            AngularDamping = 0.0f,
                            GravityFactor = 1.0f
                        };
                        motionVelocities[dynamicBodyIndex] = new MotionVelocity
                        {
                            LinearVelocity = bodyInfo.LinearVelocity,
                            AngularVelocity = bodyInfo.AngularVelocity,
                            InverseInertia = math.rcp(collider->MassProperties.MassDistribution.InertiaTensor * bodyInfo.Mass),
                            InverseMass = math.rcp(bodyInfo.Mass),
                            AngularExpansionFactor = collider->MassProperties.AngularExpansionFactor
                        };

                        indexMap.Add(i, dynamicBodyIndex);
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
                    }
                }
            }

            // Create default static body
            unsafe
            {
                staticBodies[staticBodyIndex] = new RigidBody
                {
                    WorldFromBody = new RigidTransform(quaternion.identity, float3.zero),
                    Collider = default,
                    Entity = Entity.Null,
                    CustomTags = 0
                };
            }
        }

        internal static void CreateJoints(SimulationStepInput input,
            NativeList<JointInfo> jointInfos, NativeHashMap<int, int> indexMap)
        {
            NativeSlice<Unity.Physics.Joint> joints = input.World.Joints;

            for (int i = 0; i < jointInfos.Length; i++)
            {
                var jointInfo = jointInfos[i];

                indexMap.TryGetValue(jointInfo.BodyAIndex, out int bodyAIndex);
                indexMap.TryGetValue(jointInfo.BodyBIndex, out int bodyBIndex);

                BodyIndexPair pair = new BodyIndexPair
                {
                    BodyAIndex = bodyAIndex,
                    BodyBIndex = bodyBIndex
                };

                int enabledCollisions = jointInfo.EnabledCollisions ? 1 : 0;

                joints[i] = new Unity.Physics.Joint
                {
                    JointData = jointInfo.JointData,
                    BodyPair = pair,
                    Entity = Entity.Null,
                    EnableCollision = enabledCollisions
                };
            }
        }

        internal static void ExportData(SimulationStepInput input, NativeList<BodyInfo> bodies)
        {
            int dynamicBodyIndex = 0;
            for (int i = 0; i < bodies.Length; i++)
            {
                BodyInfo bodyInfo = bodies[i];
                if (!bodyInfo.IsDynamic)
                {
                    continue;
                }

                MotionData md = input.World.MotionDatas[dynamicBodyIndex];
                RigidTransform worldFromBody = math.mul(md.WorldFromMotion, math.inverse(md.BodyFromMotion));

                bodyInfo.Position = worldFromBody.pos;
                bodyInfo.Orientation = worldFromBody.rot;
                bodyInfo.LinearVelocity = input.World.MotionVelocities[dynamicBodyIndex].LinearVelocity;
                bodyInfo.AngularVelocity = input.World.MotionVelocities[dynamicBodyIndex].AngularVelocity;

                bodies[i] = bodyInfo;
                dynamicBodyIndex++;
            }
        }

        public void Execute()
        {
            // Create the physics world
            CreateRigidBodiesAndMotions(Input, Bodies, IndexMap);
            CreateJoints(Input, Joints, IndexMap);

            // Build the broadphase
            Input.World.CollisionWorld.BuildBroadphase(ref Input.World, Input.TimeStep, Input.Gravity);

            // Step the physics world
            Simulation.StepImmediate(Input, ref SimulationContext);

            // Export the changed motion data to body info
            ExportData(Input, Bodies);
        }
    }

#if HAVOK_PHYSICS_EXISTS
    private struct SingleThreadedPhysicsHavokSimulationJob : IJob
    {
        public SimulationStepInput Input;
        public Havok.Physics.SimulationContext SimulationContext;

        public NativeList<BodyInfo> Bodies;
        public NativeList<JointInfo> Joints;

        public NativeHashMap<int, int> IndexMap;

        public void Execute()
        {
            // Create the physics world
            SingleThreadedPhysicsSimulationJob.CreateRigidBodiesAndMotions(Input, Bodies, IndexMap);
            SingleThreadedPhysicsSimulationJob.CreateJoints(Input, Joints, IndexMap);

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
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext = new Havok.Physics.SimulationContext(Havok.Physics.HavokConfiguration.Default);
#endif

        m_BodyIndexToGameObjectMapping = new Dictionary<int, GameObject>();
        m_Bodies = new NativeList<BodyInfo>(Allocator.Persistent);
        m_Joints = new NativeList<JointInfo>(Allocator.Persistent);
        PhysicsWorld = new PhysicsWorld(0, 0, 0);

        GameObject[] objects = FindObjectsOfType<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            var basicBodyInfo = objects[i].GetComponent<BasicBodyInfo>();
            if (basicBodyInfo == null)
            {
                continue;
            }

            var body = CreateBody(objects[i]);
            if (body.IsDynamic)
            {
                m_NumDynamicBodies++;
            }
            else
            {
                m_NumStaticBodies++;
            }
            m_BodyIndexToGameObjectMapping.Add(m_Bodies.Length, objects[i]);
            m_Bodies.Add(body);
        }
        CreateRagdoll();
    }

    private void CreateRagdoll()
    {
        GameObject[] objects = FindObjectsOfType<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            BasicRagdollJoint joint = objects[i].GetComponent<BasicRagdollJoint>();
            if (joint == null)
            {
                continue;
            }

            Assert.IsTrue(joint.ConnectedGameObject != null);

            GameObject bodyA = joint.ConnectedGameObject;
            GameObject bodyB = objects[i];

            bool enableCollisions = false;

            BlobAssetReference<JointData> hinge = default;
            BlobAssetReference<JointData> jointData0 = default;
            BlobAssetReference<JointData> jointData1 = default;

            switch (joint.Type)
            {
                case BasicRagdollJoint.RagdollDemoJointType.Neck:
                    {
                        CreateNeck(bodyA, bodyB, out jointData0, out jointData1);
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Shoulder:
                    {
                        CreateShoulder(bodyA, bodyB, out jointData0, out jointData1);
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Elbow:
                    {
                        hinge = CreateElbow(bodyA, bodyB);
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Wrist:
                    {
                        hinge = CreateWrist(bodyA, bodyB);
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Waist:
                    {
                        CreateWaist(bodyA, bodyB, out jointData0, out jointData1);
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Hip:
                    {
                        CreateHip(bodyA, bodyB, out jointData0, out jointData1);
                        enableCollisions = true;
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Knee:
                    {
                        hinge = CreateKnee(bodyA, bodyB);
                    }
                    break;
                case BasicRagdollJoint.RagdollDemoJointType.Ankle:
                    {
                        hinge = CreateAnkle(bodyA, bodyB);
                    }
                    break;
                default:
                    break;
            }

            GetBodyIndices(bodyA, bodyB, out int bodyAIndex, out int bodyBIndex);

            if (hinge != default)
            {
                CreateJointInfo(hinge, bodyAIndex, bodyBIndex, enableCollisions);
            }

            if (jointData0 != default)
            {
                CreateJointInfo(jointData0, bodyAIndex, bodyBIndex, enableCollisions);
            }

            if (jointData1 != default)
            {
                CreateJointInfo(jointData1, bodyAIndex, bodyBIndex, enableCollisions);
            }
        }
    }

    private void GetBodyIndices(GameObject bodyA, GameObject bodyB, out int bodyAIndex, out int bodyBIndex)
    {
        bodyAIndex = bodyBIndex = -1;

        for (int i = 0; i < m_Bodies.Length; i++)
        {
            if (m_BodyIndexToGameObjectMapping.TryGetValue(i, out GameObject go))
            {
                if (go == bodyA)
                {
                    bodyAIndex = i;
                }

                if (go == bodyB)
                {
                    bodyBIndex = i;
                }
            }
        }
    }

    private void CreateJointInfo(BlobAssetReference<JointData> jointData, int bodyAIndex, int bodyBIndex, bool enabledCollisions)
    {
        m_Joints.Add(new JointInfo
        {
            JointData = jointData,
            BodyAIndex = bodyAIndex,
            BodyBIndex = bodyBIndex,
            EnabledCollisions = enabledCollisions
        });
        m_NumJoints++;
    }

    private void OnDestroy()
    {
        if (m_Bodies.IsCreated)
        {
            m_Bodies.Dispose();
        }

        if (m_Joints.IsCreated)
        {
            m_Joints.Dispose();
        }

        PhysicsWorld.Dispose();
        SimulationContext.Dispose();
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext.Dispose();
#endif
    }
}