using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode.HostMigration;
using Unity.Transforms;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class BarrelSpawnerSystem : SystemBase
    {
        private EntityCommandBufferSystem m_CommandBuffer;

        protected override void OnCreate()
        {
            RequireForUpdate<EnableOptimization>();
            m_CommandBuffer = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        }

        partial struct SpawnJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public BarrelSetup setup;
            void Execute(Entity entity, in BarrelSpawner spawner)
            {
                SpiralPattern(ecb, spawner, setup.AmountOfCircles, setup.Spacing);

                ecb.DestroyEntity(entity);
            }
        }

        protected override void OnUpdate()
        {
            // No need to spawn anything if this is running during a host migration
            if (SystemAPI.HasSingleton<HostMigrationInProgress>())
            {
                Enabled = false;
                return;
            }

            EntityCommandBuffer ecb = m_CommandBuffer.CreateCommandBuffer();
            BarrelSetup setup = SystemAPI.GetSingleton<BarrelSetup>();

            Dependency = new SpawnJob()
            {
                ecb = ecb,
                setup = setup,
            }.Schedule(Dependency);
            m_CommandBuffer.AddJobHandleForProducer(Dependency);

            Enabled = false;
        }

        private static void SpiralPattern(EntityCommandBuffer ecb, BarrelSpawner spawner, int patternSize, float spacing)
        {
            var rand = Random.CreateFromIndex(301571925u);

            // Declare a square matrix
            int row = 2 * patternSize - 1;
            int column = 2 * patternSize - 1;

            for (int k = 0; k < patternSize; k++)
            {
                // store the first row
                // from 1st column to last column
                var j = k;
                while (j < column - k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, k, j, column, ref rand);
                    j++;
                }

                // store the last column
                // from top to bottom
                var i = k + 1;
                while (i < row - k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, i, row - 1 - k, column, ref rand);
                    i++;
                }

                // store the last row
                // from last column to 1st column
                j = column - k - 2;
                while (j >= k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, column - k - 1, j, column, ref rand);
                    j--;
                }

                // store the first column
                // from bottom to top
                i = row - k - 2;
                while (i > k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, i, k, column, ref rand);
                    i--;
                }
            }
        }

        private static void SpawnEntityAt(
            EntityCommandBuffer ecb,
            BarrelSpawner spawner,
            float spacing,
            int x, int z,
            int rowLength,
            ref Random rand)
        {
            var isRareChanceToSpawnBarrelWithoutImportanceScaling = rand.NextFloat() < 0.01f;
            var entity = ecb.Instantiate(isRareChanceToSpawnBarrelWithoutImportanceScaling ? spawner.BarrelWithoutImportance : spawner.Barrel);
            int centerAdjustment = rowLength == 1 ? 1 : (int)(math.floor(rowLength * 0.5f) * spacing);
            ecb.AddComponent(entity, LocalTransform.FromPosition(x * spacing - centerAdjustment, 0, z * spacing - centerAdjustment));
        }
    }
}
