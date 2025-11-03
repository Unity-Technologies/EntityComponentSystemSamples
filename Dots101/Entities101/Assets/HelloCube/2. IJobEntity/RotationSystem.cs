using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.JobEntity
{
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteIJobEntity>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new RotateAndScaleJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime, ElapsedTime = (float)SystemAPI.Time.ElapsedTime
            };
            job.Schedule();
        }
    }

    [BurstCompile]
    partial struct RotateAndScaleJob : IJobEntity
    {
        public float DeltaTime;
        public float ElapsedTime;

        // In source generation, a query is created from the parameters of Execute().
        // Here, the query will match all entities having a LocalTransform, PostTransformMatrix, and RotationSpeed component.
        // (In the scene, the root cube has a non-uniform scale, so it is given a PostTransformMatrix component in baking.)
        void Execute(ref LocalTransform transform, ref PostTransformMatrix postTransform, in RotationSpeed speed)
        {
            transform = transform.RotateY(speed.RadiansPerSecond * DeltaTime);
            postTransform.Value = float4x4.Scale(1, math.sin(ElapsedTime), 1);
        }
    }
}
