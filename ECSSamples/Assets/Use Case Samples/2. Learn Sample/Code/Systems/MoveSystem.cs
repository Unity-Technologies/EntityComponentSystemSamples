using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace LearnSample
{
    public partial class MoveSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = Time.DeltaTime;
            Entities
                .WithName("LearnSample_MoveSystem")
                .ForEach((
                    ref Translation translation,
                    ref Rotation rotation,
                    in TargetPosComponent targetPos,
                    in MoveSpeedComponent moveSpeed
                ) =>
                {
                    var moveVector = targetPos.Value - translation.Value;
                    var moveDir = math.normalize(moveVector);
                    rotation.Value = quaternion.LookRotation(moveDir, math.up());
                    translation.Value += moveDir * moveSpeed.value * deltaTime;
                }
                ).ScheduleParallel();
        }
    }
}