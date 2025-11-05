using System.IO;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Physics.Tests;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct MotorDataSystem : ISystem, ISystemStartStop
{
    private ComponentLookup<LocalTransform> TransformLookup;
    private ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
    private ComponentLookup<PhysicsMass> PhysicsMassLookup;

    private float ElapsedTime;
    private int AllocateSize;

    private NativeArray<float3> VelocityResults;
    private NativeArray<float3> PositionResults;
    private NativeArray<float> Time;
    private float3 StartingPosition;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MotorValidationSettings>();
        state.RequireForUpdate<PhysicsWorldSingleton>();

        TransformLookup = state.GetComponentLookup<LocalTransform>(true);
        PhysicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>(true);
        PhysicsMassLookup = state.GetComponentLookup<PhysicsMass>(true);

        ElapsedTime = 0.0f;
        AllocateSize = 1024;
    }

    public void OnStartRunning(ref SystemState state)
    {
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var(joint, entity) in SystemAPI.Query<RefRO<PhysicsJoint>>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new QuantitativeData
            {
                FrameCounter = 0,
                AccumulateData = true,
                DataWritten = false
            });
        }

        ecb.Playback(state.EntityManager);

        VelocityResults = new NativeArray<float3>(AllocateSize, Allocator.Persistent);
        PositionResults = new NativeArray<float3>(AllocateSize, Allocator.Persistent);
        Time = new NativeArray<float>(AllocateSize, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<MotorValidationSettings>();
        if (!settings.EnableValidation)
        {
            return;
        }

        var elapsedTime = ElapsedTime;
        ElapsedTime += SystemAPI.Time.DeltaTime;

        if (settings.StartTime <= elapsedTime)
        {
            TransformLookup.Update(ref state);
            PhysicsVelocityLookup.Update(ref state);
            PhysicsMassLookup.Update(ref state);

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            var accumulateJobHandle = new AccumulateDataJob()
            {
                VelocityResults = VelocityResults,
                PositionResults = PositionResults,
                Time = Time,
                ElapsedTime = elapsedTime,
                AllocateSize = AllocateSize,

                TransformLookup = TransformLookup,
                PhysicsVelocityLookup = PhysicsVelocityLookup,
                PhysicsMassLookup = PhysicsMassLookup,
                DynamicsWorld = physicsWorld.DynamicsWorld
            }.Schedule(state.Dependency);

            var exportJobHandle = new ExportDataJob()
            {
                VelocityResults = VelocityResults,
                PositionResults = PositionResults,
                Time = Time,
                AllocateSize = AllocateSize
            }.Schedule(accumulateJobHandle);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, exportJobHandle);
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (VelocityResults.IsCreated) VelocityResults.Dispose();
        if (PositionResults.IsCreated) PositionResults.Dispose();
        if (Time.IsCreated) Time.Dispose();
    }

    public partial struct AccumulateDataJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
        [ReadOnly] public DynamicsWorld DynamicsWorld;

        public NativeArray<float3> VelocityResults;
        public NativeArray<float3> PositionResults;
        public NativeArray<float> Time;
        public float ElapsedTime;
        public int AllocateSize;

        public void Execute(in Entity entity, in PhysicsJoint joint, in PhysicsConstrainedBodyPair bodyPair,
            ref QuantitativeData quantitativeData)
        {
            var target = 0.0f;
            if (joint.JointType == JointType.AngularVelocityMotor)
            {
                var constraints = joint.GetConstraints();
                target = constraints[2].Target.x;
            }

            if (!quantitativeData.AccumulateData) return;

            var frameCount = quantitativeData.FrameCounter;
            if (frameCount < AllocateSize)
            {
                // Section copied from ValidateJointBehaviorJob.Execute:
                var jointIndex = DynamicsWorld.GetJointIndex(entity);
                var dynamicsJoint = DynamicsWorld.Joints[jointIndex];
                var bodyAIx = dynamicsJoint.BodyPair.BodyIndexA;
                var bodyBIx = dynamicsJoint.BodyPair.BodyIndexB;

                var bodyAIsStatic = bodyAIx < 0 || bodyAIx >= DynamicsWorld.NumMotions;
                var bodyBIsStatic = bodyBIx < 0 || bodyBIx >= DynamicsWorld.NumMotions;
                if (bodyAIsStatic && bodyBIsStatic)
                {
                    return;
                }

                var bodyAWorld = bodyPair.EntityA != Entity.Null
                    ? TransformLookup[bodyPair.EntityA].ToMatrix()
                    : float4x4.identity;
                var bodyBWorld = bodyPair.EntityB != Entity.Null
                    ? TransformLookup[bodyPair.EntityB].ToMatrix()
                    : float4x4.identity;

                var wA = bodyAIsStatic
                    ? float3.zero
                    : PhysicsVelocityLookup[bodyPair.EntityA].GetAngularVelocityWorldSpace(
                    PhysicsMassLookup[bodyPair.EntityA],
                    new quaternion(bodyAWorld));
                var wB = bodyBIsStatic
                    ? float3.zero
                    : PhysicsVelocityLookup[bodyPair.EntityB].GetAngularVelocityWorldSpace(
                    PhysicsMassLookup[bodyPair.EntityB],
                    new quaternion(bodyBWorld));

                // actual angular velocity in world space (relative to B)
                var angVelRel = wA - wB;

                var velocity = angVelRel - new float3(0, 0, target);
                var position = TransformLookup[bodyPair.EntityA].Position;

                VelocityResults[frameCount] = velocity;
                PositionResults[frameCount] = position;
                Time[frameCount] = ElapsedTime;

                UnityEngine.Debug.Log($"Frame: {frameCount}, Velocity: {angVelRel}, Error: {velocity}");
            }
            else if (frameCount == AllocateSize)
            {
                quantitativeData.AccumulateData = false;
            }

            quantitativeData.FrameCounter = ++frameCount;
        }
    }

    public partial struct ExportDataJob : IJobEntity
    {
        public NativeArray<float3> VelocityResults;
        public NativeArray<float3> PositionResults;
        public NativeArray<float> Time;
        public int AllocateSize;

        public void Execute(in Entity entity, in PhysicsJoint joint, in PhysicsConstrainedBodyPair bodyPair,
            ref QuantitativeData quantitativeData)
        {
            if (quantitativeData.AccumulateData && quantitativeData.FrameCounter <= AllocateSize) return;
            if (quantitativeData.DataWritten) return;

            const string path = "Assets/Tests/Substepping/ValidateMotorData.txt";

            System.IO.File.Delete(path); //clear contents by deleting the file
            using (var fileStream = System.IO.File.OpenWrite(path))
            using (var writer = new System.IO.StreamWriter(fileStream, Encoding.ASCII))
            {
                string label = "Time ; deltaV_z ; deltaV_x ; deltaV_y ; position_x ; position_y";
                writer.WriteLine(label);
                for (int v = 0; v < VelocityResults.Length; v++)
                {
                    string test = Time[v].ToString() + ";" + VelocityResults[v].z + ";" +
                        VelocityResults[v].x + ";" + VelocityResults[v].y + ";" +
                        PositionResults[v].x + ";" + PositionResults[v].y;
                    writer.WriteLine(test);
                }
                Debug.Log("File write is complete. Stop playmode");
            }

            quantitativeData.DataWritten = true; //only write to file once
        }
    }
}
