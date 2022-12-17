using System;
using System.Diagnostics;
using Unity.Entities;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     Stores a sorted list of player join & disconnect events (I.e. Notifications).
    ///     Events last for <see cref="EnablePlayerListsFeature.EventListEntryDurationSeconds" />.
    /// </summary>
    public struct PlayerListNotificationBuffer : IBufferElementData
    {
        public PlayerListEntry.ChangedRpc Event;
        /// <summary>Via <see cref="Stopwatch.GetTimestamp"/></summary>
        public float DurationLeft;
    }
}
