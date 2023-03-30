using Unity.Entities;
using Unity.Mathematics;
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

        protected override void OnUpdate()
        {
            var ecb = m_CommandBuffer.CreateCommandBuffer();
            var setup = SystemAPI.GetSingleton<BarrelSetup>();

            Entities
                .ForEach((Entity entity, in BarrelSpawner spawner) =>
                {
                    SpiralPattern(ecb, spawner, setup.AmountOfCircles, setup.Spacing);

                    ecb.DestroyEntity(entity);
                }).Schedule();
            m_CommandBuffer.AddJobHandleForProducer(Dependency);

            Enabled = false;
        }

        private static void SpiralPattern(EntityCommandBuffer ecb, BarrelSpawner spawner, int patternSize, int spacing)
        {
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
                    SpawnEntityAt(ecb, spawner, spacing, k, j, column);
                    j++;
                }

                // store the last column
                // from top to bottom
                var i = k + 1;
                while (i < row - k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, i, row - 1 - k, column);
                    i++;
                }

                // store the last row
                // from last column to 1st column
                j = column - k - 2;
                while (j >= k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, column - k - 1, j, column);
                    j--;
                }

                // store the first column
                // from bottom to top
                i = row - k - 2;
                while (i > k)
                {
                    SpawnEntityAt(ecb, spawner, spacing, i, k, column);
                    i--;
                }
            }
        }

        private static void SpawnEntityAt(
            EntityCommandBuffer ecb,
            BarrelSpawner spawner,
            int spacing,
            int x, int z,
            int rowLength)
        {
            var entity = ecb.Instantiate(spawner.Barrel);
            int centerAdjustment = rowLength == 1 ? 1 : (int)(math.floor(rowLength * 0.5f) * spacing);

            ecb.AddComponent(entity, LocalTransform.FromPosition(x * spacing - centerAdjustment, 0, z * spacing - centerAdjustment));

        }
    }
}
