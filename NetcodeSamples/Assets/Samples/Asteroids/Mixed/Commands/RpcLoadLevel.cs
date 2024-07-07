using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.NetCode;
using Unity.Entities;
using UnityEngine.Assertions;

public struct LevelLoadRequest : IRpcCommand
{
    public LevelComponent levelData;
}

[BurstCompile]
public struct RpcLevelLoaded : IComponentData, IRpcCommandSerializer<RpcLevelLoaded>
{
    public void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in RpcLevelLoaded data)
    {
    }

    public void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref RpcLevelLoaded data)
    {
    }

    [BurstCompile(DisableDirectCall = true)]
    [AOT.MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcLevelLoaded);
        rpcData.Deserialize(ref parameters.Reader, parameters.DeserializerState, ref rpcData);

        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, new PlayerStateComponentData());
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, default(NetworkStreamInGame));
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, default(GhostConnectionPosition));
    }

    static readonly PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
        new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return InvokeExecuteFunctionPointer;
    }
}
[UpdateInGroup(typeof(RpcCommandRequestSystemGroup))]
[CreateAfter(typeof(RpcSystem))]
[BurstCompile]
partial struct LevelLoadedRpcCommandRequestSystem : ISystem
{
    RpcCommandRequest<RpcLevelLoaded, RpcLevelLoaded> m_Request;
    [BurstCompile]
    struct SendRpc : IJobChunk
    {
        public RpcCommandRequest<RpcLevelLoaded, RpcLevelLoaded>.SendRpcData data;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);
            data.Execute(chunk, unfilteredChunkIndex);
        }
    }
    public void OnCreate(ref SystemState state)
    {
        m_Request.OnCreate(ref state);
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var sendJob = new SendRpc{data = m_Request.InitJobData(ref state)};
        state.Dependency = sendJob.Schedule(m_Request.Query, state.Dependency);
    }
}
