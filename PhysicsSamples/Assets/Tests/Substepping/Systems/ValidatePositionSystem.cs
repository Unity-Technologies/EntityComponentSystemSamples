using System.IO;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Tests;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
partial struct ValidatePositionSystem : ISystem, ISystemStartStop
{
    private ComponentLookup<LocalTransform> TransformLookup;
    private ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;

    private float ElapsedTime;
    private int AllocateSize;

    private NativeArray<float3> LinearVelocityResults;
    private NativeArray<float3> AngularVelocityResults;
    private NativeArray<float3> PositionResults;
    private NativeArray<float> Time;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ValidatePosition>();
        state.RequireForUpdate<PhysicsWorldSingleton>();

        TransformLookup = state.GetComponentLookup<LocalTransform>(true);
        PhysicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>(true);

        ElapsedTime = 0.0f;
    }

    public void OnStartRunning(ref SystemState state)
    {
        AllocateSize = 10;
        foreach (var(cube, entity) in SystemAPI.Query<RefRO<ValidatePosition>>().WithEntityAccess())
        {
            AllocateSize = math.max(cube.ValueRO.AcquireDataSteps, AllocateSize);
        }

        LinearVelocityResults = new NativeArray<float3>(AllocateSize, Allocator.Persistent);
        AngularVelocityResults = new NativeArray<float3>(AllocateSize, Allocator.Persistent);
        PositionResults = new NativeArray<float3>(AllocateSize, Allocator.Persistent);
        Time = new NativeArray<float>(AllocateSize, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<ValidatePosition>();
        if (!settings.EnableValidation)
        {
            return;
        }

        var elapsedTime = ElapsedTime;
        ElapsedTime += SystemAPI.Time.DeltaTime;
        var maxTime = settings.AcquireDataSteps * SystemAPI.Time.DeltaTime;
        if (elapsedTime <= maxTime)
        {
            TransformLookup.Update(ref state);
            PhysicsVelocityLookup.Update(ref state);

            var accumulateJobHandle = new AccumulateDataJob()
            {
                LinearVelocityResults = LinearVelocityResults,
                AngularVelocityResults = AngularVelocityResults,
                PositionResults = PositionResults,
                Time = Time,
                ElapsedTime = elapsedTime,
                AllocateSize = AllocateSize,

                TransformLookup = TransformLookup,
                PhysicsVelocityLookup = PhysicsVelocityLookup,
            }.Schedule(state.Dependency);

            var exportJobHandle = new ExportDataJob()
            {
                LinearVelocityResults = LinearVelocityResults,
                AngularVelocityResults = AngularVelocityResults,
                PositionResults = PositionResults,
                Time = Time,
                AllocateSize = AllocateSize,
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
        if (LinearVelocityResults.IsCreated) LinearVelocityResults.Dispose();
        if (AngularVelocityResults.IsCreated) AngularVelocityResults.Dispose();
        if (PositionResults.IsCreated) PositionResults.Dispose();
        if (Time.IsCreated) Time.Dispose();
    }
}

public partial struct AccumulateDataJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    [ReadOnly] public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;

    public NativeArray<float3> LinearVelocityResults;
    public NativeArray<float3> AngularVelocityResults;
    public NativeArray<float3> PositionResults;
    public NativeArray<float> Time;
    public float ElapsedTime;
    public int AllocateSize;

    private void Execute(in Entity entity, in ValidatePosition validate, ref QuantitativeData quantitativeData)
    {
        if (!quantitativeData.AccumulateData) return;

        var frameCount = quantitativeData.FrameCounter;
        if (frameCount < AllocateSize)
        {
            var position = TransformLookup[entity].Position;
            var velocity = PhysicsVelocityLookup[entity];

            LinearVelocityResults[frameCount] = velocity.Linear;
            AngularVelocityResults[frameCount] = velocity.Angular;
            PositionResults[frameCount] = position;
            Time[frameCount] = ElapsedTime;

            UnityEngine.Debug.Log($"Frame: {frameCount}, Position: {position}");
        }
        else if (frameCount >= AllocateSize)
        {
            Debug.Log("Time expired");
            quantitativeData.AccumulateData = false;
        }

        quantitativeData.FrameCounter = ++frameCount;
    }
}

public partial struct ExportDataJob : IJobEntity
{
    public NativeArray<float3> LinearVelocityResults;
    public NativeArray<float3> AngularVelocityResults;
    public NativeArray<float3> PositionResults;
    public NativeArray<float> Time;
    public int AllocateSize;

    private void Execute(in Entity entity, in ValidatePosition validate, ref QuantitativeData quantitativeData)
    {
        if (quantitativeData.AccumulateData && quantitativeData.FrameCounter < AllocateSize) return;
        if (quantitativeData.DataWritten) return;

        const string path = "Assets/Tests/Substepping/ValidatePositionSystemResults.txt";

        System.IO.File.Delete(path);         //clear contents by deleting the file
        using (var fileStream = System.IO.File.OpenWrite(path))
        using (var writer = new System.IO.StreamWriter(fileStream, Encoding.ASCII))
        {
            var startingPoint = PositionResults[0];
            string label = "Time ; distance ; speed ; position_x ; position_y ; position_z ; linearVelocity_x ; linearVelocity_y ; linearVelocity_z ; angularVelocity_x ; angularVelocity_y ; angularVelocity_z";
            writer.WriteLine(label);
            for (int i = 0; i < PositionResults.Length; i++)
            {
                var distance = PositionResults[i] - startingPoint;
                var length = math.length(distance);
                var speed = math.length(LinearVelocityResults[i]);
                string test = Time[i] + ";" + length + ";" + speed + ";" +
                    PositionResults[i].x + ";" + PositionResults[i].y + ";" + PositionResults[i].z + ";" +
                    LinearVelocityResults[i].x + ";" + LinearVelocityResults[i].y + ";" +
                    LinearVelocityResults[i].z + ";" +
                    AngularVelocityResults[i].x + ";" + AngularVelocityResults[i].y + ";" + AngularVelocityResults[i].z;
                writer.WriteLine(test);
            }
            Debug.Log("File write is complete. Stop playmode");
        }

        quantitativeData.DataWritten = true;     //only write to file once
    }
}
