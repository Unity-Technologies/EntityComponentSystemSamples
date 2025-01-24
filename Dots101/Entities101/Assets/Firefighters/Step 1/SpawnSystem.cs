using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Firefighters
{
    public partial struct SpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var config = SystemAPI.GetSingleton<Config>();
            var rand = new Random(123);

            var bucketEntities = new NativeArray<Entity>(config.NumBuckets, Allocator.Temp);
            
            // spawn buckets
            {
                // struct components are returned and passed by value (as copies)!
                var bucketTransform = state.EntityManager.GetComponentData<LocalTransform>(config.BucketPrefab);
                bucketTransform.Position.y = (bucketTransform.Scale / 2); // will be same for every bucket

                for (int i = 0; i < config.NumBuckets; i++)
                {
                    var bucketEntity = state.EntityManager.Instantiate(config.BucketPrefab);
                    bucketEntities[i] = bucketEntity;

                    bucketTransform.Position.x = rand.NextFloat(0.5f, config.GroundNumColumns - 0.5f);
                    bucketTransform.Position.z = rand.NextFloat(0.5f, config.GroundNumRows - 0.5f);
                    bucketTransform.Scale = config.BucketEmptyScale;

                    state.EntityManager.SetComponentData(bucketEntity, bucketTransform);
                }
            }

            // spawn teams
            {
                int numBotsPerTeam = config.NumPassersPerTeam + 1;
                int douserIdx = (config.NumPassersPerTeam / 2);
                for (int teamIdx = 0; teamIdx < config.NumTeams; teamIdx++)
                {
                    var teamEntity = state.EntityManager.CreateEntity();
                    var team = new Team
                    {
                        Bucket = bucketEntities[teamIdx]
                    };
                    state.EntityManager.AddComponent<RepositionLine>(teamEntity);
                    var memberBuffer = state.EntityManager.AddBuffer<TeamMember>(teamEntity);
                    memberBuffer.Capacity = numBotsPerTeam;
                    var teamColor = new float4(rand.NextFloat3(), 1);

                    // spawn bots
                    for (int botIdx = 0; botIdx < numBotsPerTeam; botIdx++)
                    {
                        var botEntity = state.EntityManager.Instantiate(config.BotPrefab);

                        var x = rand.NextFloat(0.5f, config.GroundNumColumns - 0.5f);
                        var z = rand.NextFloat(0.5f, config.GroundNumRows - 0.5f);

                        state.EntityManager.SetComponentData(botEntity, LocalTransform.FromPosition(x, 1, z));
                        state.EntityManager.SetComponentData(botEntity, new URPMaterialPropertyBaseColor
                        {
                            Value = teamColor
                        });

                        // designate the filler
                        if (botIdx == 0)
                        {
                            team.Filler = botEntity;
                        }

                        memberBuffer.Add(new TeamMember { Bot = botEntity });
                    }

                    // connect each bot to the next in line, forming a passing ring
                    for (int botIdx = 0; botIdx < memberBuffer.Length; botIdx++)
                    {
                        Entity nextBot;
                        if (botIdx == memberBuffer.Length - 1)
                        {
                            nextBot = memberBuffer[0].Bot;   // next is filler
                        }
                        else
                        {
                            nextBot = memberBuffer[botIdx + 1].Bot;
                        }

                        state.EntityManager.SetComponentData(memberBuffer[botIdx].Bot, new Bot
                        {
                            NextBot = nextBot,
                            Team = teamEntity,
                            IsFiller = (botIdx == 0),
                            IsDouser = (botIdx == douserIdx),
                        });
                    }

                    state.EntityManager.AddComponentData(teamEntity, team);
                }
            }

            // spawn ponds
            {
                var bounds = new NativeArray<float4>(4, Allocator.Temp);

                const float innerMargin = 2; // margin between ground edge and pond area
                const float outerMargin = innerMargin + 3;
                float width = config.GroundNumColumns;
                float height = config.GroundNumRows;

                // 4 sides around the field of ground cells
                // x, y is bottom-left corner; z, w is top-right corner
                bounds[0] = new float4(0.5f, -outerMargin, width - 0.5f, -innerMargin); // bottom
                bounds[1] = new float4(0.5f, height + innerMargin, width - 0.5f, height + outerMargin); // top
                bounds[2] = new float4(-outerMargin, 0.5f, -innerMargin, height - 0.5f); // left
                bounds[3] = new float4(width + innerMargin, 0.5f, width + outerMargin, height - 0.5f); // right

                var pondTransform = state.EntityManager.GetComponentData<LocalTransform>(config.PondPrefab);
                for (int i = 0; i < 4; i++)
                {
                    var bottomLeft = bounds[i].xy;
                    var topRight = bounds[i].zw;

                    for (int j = 0; j < config.NumPondsPerEdge; j++)
                    {
                        var pondEntity = state.EntityManager.Instantiate(config.PondPrefab);

                        var pos = rand.NextFloat2(bottomLeft, topRight);
                        pondTransform.Position = new float3(pos.x, 0, pos.y);
                        state.EntityManager.SetComponentData(pondEntity, pondTransform);
                    }
                }
            }

            // spawn field
            {
                var groundCellTransform = state.EntityManager.GetComponentData<LocalTransform>(config.GroundCellPrefab);
                groundCellTransform.Position.y = -(config.GroundCellYScale / 2);

                for (int column = 0; column < config.GroundNumColumns; column++)
                {
                    for (int row = 0; row < config.GroundNumRows; row++)
                    {
                        var groundCellEntity = state.EntityManager.Instantiate(config.GroundCellPrefab);
                        groundCellTransform.Position.x = column + 0.5f;
                        groundCellTransform.Position.z = row + 0.5f;
                        state.EntityManager.SetComponentData(groundCellEntity, groundCellTransform);
                        state.EntityManager.SetComponentData(groundCellEntity, new URPMaterialPropertyBaseColor
                        {
                            Value = config.MinHeatColor
                        });
                    }
                }
            }

            // spawn heat map
            {
                var entity = state.EntityManager.CreateEntity();
                var heatBuffer = state.EntityManager.AddBuffer<Heat>(entity);

                // init the heat buffer
                {
                    heatBuffer.Length = config.GroundNumColumns * config.GroundNumRows;
                    // set every cell to zero
                    for (int i = 0; i < heatBuffer.Length; i++)
                    {
                        heatBuffer[i] = new Heat { Value = 0f };
                    }
                }

                // set some random cells on fire
                {
                    for (int i = 0; i < config.NumInitialCellsOnFire; i++)
                    {
                        var randomIdx = rand.NextInt(0, heatBuffer.Length);
                        heatBuffer[randomIdx] = new Heat { Value = 1f };
                    }
                }

                // Move the ground cells so that their query iteration order corresponds to indexes of the heat buffer.
                // (As long as the set of entities matched by the query stays the same, the query iteration order will remain the same.
                // So, this will make it easy/fast to update the color and height of the ground cells from the heat data.)
                {
                    var x = 0;
                    var z = 0;

                    foreach (var trans in
                             SystemAPI.Query<RefRW<LocalTransform>>()
                                 .WithAll<GroundCell>())
                    {
                        trans.ValueRW.Position.x = x + 0.5f;
                        trans.ValueRW.Position.z = z + 0.5f;

                        x++;
                        if (x >= config.GroundNumColumns)
                        {
                            x = 0;
                            z++;
                        }
                    }
                }
            }
        }
    }
}
