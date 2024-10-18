using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     Stores <see cref="ChangedRpc.UpdateType"/> for a given "NetworkConnection" entity.
    ///     See <see cref="ServerPlayerListSystem" /> and <see cref="ClientPlayerListSystem" />.
    /// </summary>
    public struct PlayerListEntry : ICleanupComponentData
    {
        /// <remarks>Sent by client.</remarks>
        public struct ClientRegisterUsernameRpc : IRpcCommand
        {
            public FixedString64Bytes Value;
        }

        /// <summary>Sent by the server to a client if the clients username is invalid.</summary>
        public struct InvalidUsernameResponseRpc : IRpcCommand
        {
            public FixedString64Bytes RequestedUsername;
        }

        /// <remarks>Sent by server any time the clients username or state changes.</remarks>
        public struct ChangedRpc : IRpcCommand
        {
            /// <inheritdoc cref="Reason" />
            public bool IsConnected => ChangeType != UpdateType.PlayerDisconnect;

            public enum UpdateType : byte
            {
                /// <summary>A player (who I'm aware of) disconnected.</summary>
                PlayerDisconnect = 0,
                /// <summary>This is a new joiner, who joined AFTER me.</summary>
                NewJoiner,
                /// <summary>This is an existing player, who joined the game BEFORE me.</summary>
                ExistingPlayer,
                /// <summary>A player changed their username.</summary>
                UsernameChange,
            }

            public UpdateType ChangeType;

            /// <remarks>Client cannot infer NetworkId as this RPC is sent from the server.</remarks>
            public int NetworkId;

            public ClientRegisterUsernameRpc Username;

            /// <summary>
            ///     Invalid while <see cref="IsConnected" />. Stores the last disconnect reason / cause for this client, allowing
            ///     other players to display the reason.
            /// </summary>
            public NetworkStreamDisconnectReason Reason;
        }

        /// <summary>Stores the last received RPC for this player.</summary>
        public ChangedRpc State;

        public bool IsCreated => State.NetworkId != default;
    }
}
