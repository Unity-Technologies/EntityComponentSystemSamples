using System;
using Unity.Entities;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     Similar to <see cref="PlayerListEntry"/>, except a <see cref="IBufferElementData"/>, allowing the client to store this data in a buffer.
    ///     Stores all players and their state (username, ping etc).
    ///     Index mapped to NetworkId - 1.
    /// </summary>
    /// <remarks>
    ///     This is therefore automatically sorted and deterministic.
    ///     Will contain disconnected players whose NetworkId has not been re-used.
    ///     Will also contain default entries as the list is resized with some spare capacity.
    ///     Depends on the implicit rules of NetworkId's.
    ///     A list is used to allow implicit resizing without having to dispose.
    /// </remarks>
    public struct PlayerListBufferEntry : IBufferElementData
    {
        /// <summary>Stores the last received RPC for this player.</summary>
        public PlayerListEntry.ChangedRpc State;

        public bool IsCreated => State.NetworkId != default;

        /// <summary>
        /// Returns the number of connected players.
        /// Why? The buffer can also contain disconnected players.
        /// </summary>
        /// <param name="playerListBufferEntries"></param>
        /// <returns></returns>
        public static int CountNumConnectedPlayers(DynamicBuffer<PlayerListBufferEntry> playerListBufferEntries)
        {
            var count = 0;
            foreach (var entry in playerListBufferEntries)
            {
                if (entry.IsCreated && entry.State.IsConnected) count++;
            }
            return count;
        }
    }
}
