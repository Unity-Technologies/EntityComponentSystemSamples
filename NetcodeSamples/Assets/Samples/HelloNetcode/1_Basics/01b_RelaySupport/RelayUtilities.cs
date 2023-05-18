using System.Collections.Generic;
using System.Linq;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Necessary wrappers around unsafe functions to convert raw data to various relay structs.
    /// </summary>
    public static class RelayUtilities
    {
        public static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        public static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        public static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }

        public static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
        {
            return endpoints.FirstOrDefault(endpoint => endpoint.ConnectionType == connectionType);
        }
    }
}
