using Unity.Entities;
using Unity.NetCode;
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

    [WithNone(typeof(LevelSync_InitializedConnection))]
    partial struct ConnectionJob : IJobEntity
    {
        public EntityCommandBuffer commandBuffer;
        void Execute(Entity entity, ref NetworkStreamConnection state)
        {
            commandBuffer.AddComponent(entity, new LevelSync_InitializedConnection());
            Debug.Log($"New connection accepted: {state.Value.ToFixedString()}");
        }
    }

    protected override void OnUpdate()
    {
        var ecb = m_CommandBuffer.CreateCommandBuffer();
        var job = new ConnectionJob()
        {
            commandBuffer = ecb
        };
        Dependency = job.Schedule(this.Dependency);
        m_CommandBuffer.AddJobHandleForProducer(Dependency);
    }
}
