using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

struct UserInputData : IComponentData
{
    public float2 Move;
}

public partial class GatherInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        Entities
            .WithName("GatherInput")
            .ForEach((ref UserInputData inputData) =>
            {
                inputData.Move = new float2(horizontal, vertical);
            }).ScheduleParallel();
    }
}
