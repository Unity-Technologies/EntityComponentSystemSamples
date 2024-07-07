using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SetAlwaysRelevantSystem : SystemBase
{
    protected override void OnCreate()
    {
        var relevancy = SystemAPI.GetSingletonRW<GhostRelevancy>();
        // This is set OnCreate but can be updated at runtime as well
        // You could also add an AlwaysRelevant component to mark entities at authoring time too
        relevancy.ValueRW.DefaultRelevancyQuery = GetEntityQuery(typeof(AsteroidScore));
    }

    protected override void OnUpdate() { }
}
