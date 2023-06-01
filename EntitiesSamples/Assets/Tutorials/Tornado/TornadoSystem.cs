using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tutorials.Tornado
{
    /*
     * Updates the swirling boxes that form the tornado.
     */
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct TornadoSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var config = SystemAPI.GetSingleton<Config>();
            new TornadoParticleJob
            {
                ParticleSpinRate = config.ParticleSpinRate,
                ParticleUpwardSpeed = config.ParticleUpwardSpeed,
                ElapsedTime = elapsedTime,
                Tornado = BuildingSystem.Position(elapsedTime),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct TornadoParticleJob : IJobEntity
    {
        public float ElapsedTime;
        public float2 Tornado;
        public float DeltaTime;
        public float ParticleSpinRate;
        public float ParticleUpwardSpeed;

        public void Execute(ref LocalTransform transform, in Particle particle)
        {
            var tornadoPos = new float3(Tornado.x + BuildingSystem.TornadoSway(transform.Position.y, ElapsedTime),
                transform.Position.y, Tornado.y);
            var delta = tornadoPos - transform.Position;
            float dist = math.length(delta);
            delta /= dist;
            float inForce = dist - math.saturate(tornadoPos.y / 50f) * 30f * particle.radiusMult + 2f;
            transform.Position += new float3(-delta.z * ParticleSpinRate + delta.x * inForce, ParticleUpwardSpeed,
                delta.x * ParticleSpinRate + delta.z * inForce) * DeltaTime;
            if (transform.Position.y > 50f)
            {
                transform.Position.y = 0f;
            }
        }
    }
}
