using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Firefighters
{
    public partial struct LineSystem : ISystem
    {
        private uint seed;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<RepositionLine>();
            state.RequireForUpdate<Heat>();
            state.RequireForUpdate<Team>();
            state.RequireForUpdate<ExecuteTeam>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var rand = new Random(123 +
                                  seed++); // seed is incremented to get different random values in different frames

            var pondQuery = SystemAPI.QueryBuilder().WithAll<Pond, LocalTransform>().Build();
            var pondPositions = pondQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            var heatBuffer = SystemAPI.GetSingletonBuffer<Heat>();

            // EnabledRefRW gives us access to the enabled state of RepositionLine.
            // The query will only match entities whose RepositionLine is enabled.
            foreach (var (team, members, respositionLineState) in
                     SystemAPI.Query<RefRO<Team>, DynamicBuffer<TeamMember>, EnabledRefRW<RepositionLine>>())
            {
                respositionLineState.ValueRW = false; // disable RepositionLine

                // set LinePos of the team's bots and set their bot state
                {
                    var randomPondPos = pondPositions[rand.NextInt(pondPositions.Length)].Position.xz;
                    var nearestFirePos = HeatSystem.NearestFire(randomPondPos, heatBuffer,
                        config.GroundNumRows, config.GroundNumColumns, config.HeatDouseTargetMin);

                    var douserIdx = members.Length / 2;

                    var vec = nearestFirePos - randomPondPos;
                    var vecNorm = math.normalize(vec);
                    var offsetVec = new float2(-vecNorm.y, vecNorm.x);

                    for (int i = 1; i <= douserIdx; i++)
                    {
                        var ratio = (float)i / (douserIdx + 1);
                        var offset = math.sin(math.lerp(0, math.PI, ratio)) * offsetVec * config.LineMaxOffset;
                        var pos = math.lerp(randomPondPos, nearestFirePos, ratio);

                        var bot = SystemAPI.GetComponentRW<Bot>(members[i].Bot);
                        bot.ValueRW.State = BotState.MOVE_TO_LINE;
                        bot.ValueRW.LinePos = pos + offset;

                        if (bot.ValueRO.IsDouser)
                        {
                            bot.ValueRW.TargetPos = nearestFirePos;
                        }

                        var otherBot = SystemAPI.GetComponentRW<Bot>(members[^i].Bot);
                        otherBot.ValueRW.State = BotState.MOVE_TO_LINE;
                        otherBot.ValueRW.LinePos = pos - offset;
                    }

                    var filler = SystemAPI.GetComponentRW<Bot>(team.ValueRO.Filler);
                    filler.ValueRW.LinePos = randomPondPos;
                    
                    var bucket = SystemAPI.GetComponentRW<Bucket>(team.ValueRO.Bucket);
                    if (bucket.ValueRO.IsCarried)
                    {
                        filler.ValueRW.State = BotState.MOVE_TO_LINE;
                    }
                    else
                    {
                        filler.ValueRW.TargetPos = SystemAPI.GetComponent<LocalTransform>(team.ValueRO.Bucket).Position.xz;
                        filler.ValueRW.State = BotState.MOVE_TO_BUCKET;
                    }
                }
            }
        }
    }
}