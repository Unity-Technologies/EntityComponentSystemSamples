using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace LearnSample
{
    public partial class MoveToNextSystem : SystemBase
    {
        //private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;
        //protected override void OnCreate()
        //{
        //    _endSimulationEntityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        //}

        protected override void OnUpdate()
        {
            Entities
                .WithName("LearnSample_MoveToNextSystem")
                .ForEach((
                    DynamicBuffer<PathPointComponent> pathPoints,
                    ref TargetPosComponent targetPos,
                    ref NextPathPointIndexComponent nextPathPointIndex,
                    in Translation translation
                ) =>
                {
                    var moveVector = targetPos.Value - translation.Value;
                    if (math.lengthsq(moveVector) < GlobalConst.kStopDistanceSq)
                    {
                        nextPathPointIndex.Value = (nextPathPointIndex.Value + 1) % pathPoints.Length;
                        targetPos.Value = pathPoints[nextPathPointIndex.Value].Value;
                    }
                }
                ).ScheduleParallel();
        }
    }
}