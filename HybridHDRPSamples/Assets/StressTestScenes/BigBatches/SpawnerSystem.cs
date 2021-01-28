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

public class SpawnerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity spawnerEntity, ref SpawnData spawndata, ref LocalToWorld localToWorld) =>
        {
            Action<float4x4, SpawnData, int, int, bool> spawn = (lToWorld, spawnData, x, y, disabled) =>
            {
                var pos = new float4(x, 0.0f, y, 1);
                pos = math.mul(lToWorld, pos);

                var cube = PostUpdateCommands.Instantiate(spawnData.Prefab);
                PostUpdateCommands.SetComponent(cube, new Translation { Value = pos.xyz });
                PostUpdateCommands.AddComponent(cube, new SpawnIndex { Value = (y*spawnData.CountX)+x, Height = spawnData.CountY });
                if(disabled)
                    PostUpdateCommands.AddComponent(cube, new DisableRendering { });
            };

            if (!spawndata.HasRenderingDisabledEntities)
            {
                for (int x = 0; x < spawndata.CountX; ++x)
                    for (int y = 0; y < spawndata.CountY; ++y)
                        spawn(localToWorld.Value, spawndata, x, y, false);
            }
            else
            {
                for (int x = 0; x < spawndata.CountX; ++x)
                {
                    for (int y = 0; y < spawndata.CountY; ++y)
                    {
                        spawn(localToWorld.Value, spawndata, x, y, false);
                        spawn(localToWorld.Value, spawndata, x, y, true);
                    }
                }
            }

            PostUpdateCommands.DestroyEntity(spawnerEntity);
        });
    }
}
