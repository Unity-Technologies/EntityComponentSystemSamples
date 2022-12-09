using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

struct SpawnIndex : IComponentData
{
    public int Value;
    public int Height;
}

public partial class SpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

        Entities.ForEach((Entity spawnerEntity, ref SpawnData spawndata, ref LocalToWorld localToWorld) =>
        {
            Action<float4x4, SpawnData, int, int, bool, EntityCommandBuffer> spawn = (lToWorld, spawnData, x, y, disabled, ecb) =>
            {
                var pos = new float4(x, 0.0f, y, 1);
                pos = math.mul(lToWorld, pos);

                var cube = ecb.Instantiate(spawnData.Prefab);
                ecb.SetComponent(cube, new Translation { Value = pos.xyz });
                ecb.AddComponent(cube, new SpawnIndex { Value = (y*spawnData.CountX)+x, Height = spawnData.CountY });
                if(disabled)
                    ecb.AddComponent(cube, new DisableRendering { });
            };

            if (!spawndata.HasRenderingDisabledEntities)
            {
                for (int x = 0; x < spawndata.CountX; ++x)
                for (int y = 0; y < spawndata.CountY; ++y)
                    spawn(localToWorld.Value, spawndata, x, y, false, cmdBuffer);
            }
            else
            {
                for (int x = 0; x < spawndata.CountX; ++x)
                {
                    for (int y = 0; y < spawndata.CountY; ++y)
                    {
                        spawn(localToWorld.Value, spawndata, x, y, false, cmdBuffer);
                        spawn(localToWorld.Value, spawndata, x, y, true, cmdBuffer);
                    }
                }
            }

            cmdBuffer.DestroyEntity(spawnerEntity);
        }).WithoutBurst().Run();

        cmdBuffer.Playback(EntityManager);
        cmdBuffer.Dispose();
    }
}
