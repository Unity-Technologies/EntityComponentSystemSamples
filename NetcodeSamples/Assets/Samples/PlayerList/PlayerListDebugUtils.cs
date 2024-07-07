using System;
using Unity.Collections;

namespace Unity.NetCode.Samples.PlayerList
{
    public static class PlayerListDebugUtils
    {
        public static FixedString128Bytes ToFixedString(PlayerListEntry entry)
        {
            var s = new FixedString128Bytes();
            s.Append((FixedString32Bytes) "PlayerListEntry['");
            s.Append(entry.State.NetworkId);
            s.Append(':');
            s.Append(',');
            switch (entry.State.ChangeType)
            {
                case PlayerListEntry.ChangedRpc.UpdateType.PlayerDisconnect:
                    s.Append((FixedString64Bytes)nameof(PlayerListEntry.ChangedRpc.UpdateType.PlayerDisconnect));
                    break;
                case PlayerListEntry.ChangedRpc.UpdateType.NewJoiner:
                    s.Append((FixedString64Bytes)nameof(PlayerListEntry.ChangedRpc.UpdateType.NewJoiner));
                    break;
                case PlayerListEntry.ChangedRpc.UpdateType.ExistingPlayer:
                    s.Append((FixedString64Bytes)nameof(PlayerListEntry.ChangedRpc.UpdateType.ExistingPlayer));
                    break;
                case PlayerListEntry.ChangedRpc.UpdateType.UsernameChange:
                    s.Append((FixedString64Bytes)nameof(PlayerListEntry.ChangedRpc.UpdateType.UsernameChange));
                    break;
                default:
                    s.Append((int)entry.State.ChangeType);
                    break;
            }
            s.Append(entry.State.Username.Value);
            s.Append(',');
            s.Append(GetConnectionStatusFixedString(entry.State));
            s.Append(']');
            return s;
        }

        public static FixedString32Bytes GetConnectionStatusFixedString(PlayerListEntry.ChangedRpc rpc)
        {
            if (rpc.IsConnected)
                return "Connected!";
            switch (rpc.Reason)
            {
                case NetworkStreamDisconnectReason.Timeout:
                    return "Timed Out!";
                case NetworkStreamDisconnectReason.MaxConnectionAttempts:
                    return "Could Not Connect!";
                case NetworkStreamDisconnectReason.ClosedByRemote:
                    return "Disconnected!";
                case NetworkStreamDisconnectReason.ConnectionClose:
                    return "ConnectionClosed!";
                case NetworkStreamDisconnectReason.BadProtocolVersion:
                    return "BadProtocolVersion!";
                case NetworkStreamDisconnectReason.InvalidRpc:
                    return "InvalidRpc!";
                case NetworkStreamDisconnectReason.AuthenticationFailure:
                    return "Authentication failure!";
                case NetworkStreamDisconnectReason.ProtocolError:
                    return "ProtocolError!";
                default:
                    var s = (FixedString32Bytes) "Err[";
                    s.Append((int) rpc.Reason);
                    s.Append(']');
                    s.Append('!');
                    return s;
            }
        }

        public static FixedString128Bytes ToFixedString(PlayerListEntry.ClientRegisterUsernameRpc rpc)
        {
            var s = (FixedString128Bytes)nameof(PlayerListEntry.ClientRegisterUsernameRpc);
            s.Append('[');
            s.Append(rpc.Value);
            s.Append(']');
            return s;
        }

        public static FixedString128Bytes ToFixedString(PlayerListEntry.ChangedRpc rpc)
        {
            if (rpc.NetworkId == 0)
                return (FixedString128Bytes)"ChangedRpc[null]";

            var s = (FixedString128Bytes) "ChangedRpc[";
            s.Append(rpc.NetworkId);
            s.Append(':');
            s.Append(GetConnectionStatusFixedString(rpc));
            s.Append(',');
            s.Append(rpc.Username.Value);
            s.Append(']');
            return s;
        }
    }
}
