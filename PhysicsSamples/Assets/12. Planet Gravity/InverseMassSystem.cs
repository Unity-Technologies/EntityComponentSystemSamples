using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace PlanetGravity
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct InverseMassSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new EntityInverseMassJob().Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(Asteroid))]
        public partial struct EntityInverseMassJob : IJobEntity
        {
            private void Execute(ref PhysicsMass mass)
            {
                var random = new Random();
                random.InitState(10);
                mass.InverseMass = random.NextFloat(mass.InverseMass, mass.InverseMass * 4f);
            }
        }
    }
}
