using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    public struct ChatMessage : IRpcCommand
    {
        public FixedString128Bytes Message;
    }

    public struct ChatUser : IRpcCommand
    {
        public int UserData;
    }

    public struct ChatUserInitialized : IComponentData { }

    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class RpcClientSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnableRPC>();
            // Can't send any RPC/chat messages before connection is established
            RequireForUpdate<NetworkId>();
        }

        protected override void OnUpdate()
        {
            // This is not set up to handle multiple clients/worlds using one or more chat windows but the
            // rpc messages must be consumed or else warnings will be emitted
            if (World.IsThinClient())
            {
                EntityManager.DestroyEntity(GetEntityQuery(new EntityQueryDesc()
                {
                    All = new[] { ComponentType.ReadOnly<ReceiveRpcCommandRequest>() },
                    Any = new[] { ComponentType.ReadOnly<ChatMessage>(), ComponentType.ReadOnly<ChatUser>() }
                }));
            }

            // When a user or chat message RPCs arrive they are added the queues for consumption
            // in the UI system.
            var buffer = new EntityCommandBuffer(Allocator.Temp);
            var connections = GetComponentLookup<NetworkId>(true);
            FixedString32Bytes worldName = World.Name;
            foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<ChatMessage>>().WithEntityAccess())
            {
                buffer.DestroyEntity(entity);

                // Not thread safe, so all UI logic is kept on main thread
                RpcUiData.Messages.Data.Enqueue(chat.ValueRO.Message);
            }

            foreach (var (rpcCmd, user, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, ChatUser>().WithEntityAccess())
            {
                var conId = connections[rpcCmd.SourceConnection].Value;
                UnityEngine.Debug.Log(
                    $"[{worldName}] Received {user.UserData} from connection {conId}");
                buffer.DestroyEntity(entity);
                RpcUiData.Users.Data.Enqueue(user.UserData);
            }

            if (!buffer.IsEmpty)
            {
                buffer.Playback(EntityManager);
            }
        }
    }

    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class RpcServerSystem : SystemBase
    {
        // User information is just tracked as a single integer (=connection ID) to make this as simple as possible
        private NativeList<int> m_Users;

        protected override void OnCreate()
        {
            RequireForUpdate<EnableRPC>();
            m_Users = new NativeList<int>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Users.Dispose();
        }

        protected override void OnUpdate()
        {
            var buffer = new EntityCommandBuffer(Allocator.Temp);
            var connections = GetComponentLookup<NetworkId>(true);
            FixedString32Bytes worldName = World.Name;

            // New incoming RPCs are placed on an entity with the ReceiveRpcCommandRequestComponent component and the RPC data payload component (ChatMessage)
            // This entity should be deleted when you're done processing it
            // The server RPC broadcasts the chat message to all connections
            foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, ChatMessage>().WithEntityAccess())
            {
                var conId = connections[rpcCmd.SourceConnection].Value;
                UnityEngine.Debug.Log(
                    $"[{worldName}] Received {chat.Message} on connection {conId}.");
                buffer.DestroyEntity(entity);
                var broadcastEntity = buffer.CreateEntity();
                buffer.AddComponent(broadcastEntity, new ChatMessage() { Message = FixedString.Format("User {0}: {1}", conId, chat.Message) });
                buffer.AddComponent<SendRpcCommandRequest>(broadcastEntity);
            }

            var users = m_Users;
            foreach (var (id, entity) in SystemAPI.Query<NetworkId>().WithEntityAccess().WithNone<ChatUserInitialized>())
            {
                var connectionId = id.Value;

                // Notify all connections about new chat user (including himself)
                var broadcastEntity = buffer.CreateEntity();
                buffer.AddComponent(broadcastEntity, new ChatUser() { UserData = connectionId });
                buffer.AddComponent<SendRpcCommandRequest>(broadcastEntity);
                UnityEngine.Debug.Log($"[{worldName}] New user 'User {connectionId}' connected. Broadcasting user entry to all connections;");

                // Notify only new connection about other users already connected, this uses the TargetConnection portion
                // of the RPC request component
                for (int i = 0; i < users.Length; ++i)
                {
                    var newEntity = buffer.CreateEntity();
                    var user = users[i];
                    buffer.AddComponent(newEntity, new ChatUser() { UserData = user });
                    buffer.AddComponent<SendRpcCommandRequest>(newEntity);
                    buffer.SetComponent(newEntity, new SendRpcCommandRequest { TargetConnection = entity });
                    UnityEngine.Debug.Log($"[{worldName}] Sending user 'User {user}' to new connection {connectionId}");
                }

                // Add connection to user list
                users.Add(connectionId);

                // Mark this connection/user so we don't process again
                buffer.AddComponent<ChatUserInitialized>(entity);
            }

            if (!buffer.IsEmpty)
            {
                buffer.Playback(EntityManager);
            }
        }
    }

    // Management for the queue which passes data between DOTS and GameObject systems, this way
    // the two are decoupled a bit cleaner
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class RpcUiDataSystem : SystemBase
    {
        private bool m_OwnsData;
        protected override void OnCreate()
        {
            m_OwnsData = !RpcUiData.Users.Data.IsCreated;
            if (m_OwnsData)
            {
                RpcUiData.Users.Data = new UnsafeQueue<int>(Allocator.Persistent);
                RpcUiData.Messages.Data = new UnsafeQueue<FixedString128Bytes>(Allocator.Persistent);
            }
            Enabled = false;
        }

        protected override void OnUpdate() { }

        protected override void OnDestroy()
        {
            if (m_OwnsData)
            {
                RpcUiData.Messages.Data.Dispose();
                RpcUiData.Users.Data.Dispose();
            }
        }
    }

    public abstract class RpcUiData
    {
        public static readonly SharedStatic<UnsafeQueue<FixedString128Bytes>> Messages = SharedStatic<UnsafeQueue<FixedString128Bytes>>.GetOrCreate<MessagesKey>();
        public static readonly SharedStatic<UnsafeQueue<int>> Users = SharedStatic<UnsafeQueue<int>>.GetOrCreate<UsersKey>();

        // Identifiers for the shared static fields
        private class MessagesKey {}
        private class UsersKey {}
    }
}
