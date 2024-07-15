using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

public struct LevelSync_InitializedConnection : IComponentData
{
}

[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class LevelSync_ServerConnectionSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_CommandBuffer;

    protected override void OnCreate()
    {
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!(sceneName.Contains("LevelSync") || sceneName.Contains("JumpOff")))
            Enabled = false;

        m_CommandBuffer = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_CommandBuffer.CreateCommandBuffer();
        Entities.WithNone<LevelSync_InitializedConnection>().ForEach((Entity entity, ref NetworkStreamConnection state) =>
        {
            commandBuffer.AddComponent(entity, new LevelSync_InitializedConnection());
            Debug.Log($"New connection accepted: {state.Value.ToFixedString()}");
        }).Schedule();
        m_CommandBuffer.AddJobHandleForProducer(Dependency);

    }
}
