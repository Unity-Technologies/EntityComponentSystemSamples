using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

//[UpdateAfter(typeof(GatherInputSystem))]
//public partial class RotatePlayerSystem : SystemBase
//{
//    protected override void OnUpdate()
//    {
//        var deltaTime = Time.DeltaTime;
//        Entities
//            .WithName("RotatePlayer")
//            .ForEach((
//                ref Rotation rotation,
//                in UserInputData input,
//                in RotationSpeed speed) =>
//                {
//                    var forward = new float3(input.Move.x, 0.0f, input.Move.y);
//                    forward = math.normalize(forward);
//                    var targerRot = quaternion.LookRotation(forward, math.up());
//                    rotation.Value = math.slerp(rotation.Value, targerRot, speed.RadiansPerSecond * deltaTime);
//                }).ScheduleParallel();
//    }
//}
