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
        public static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
        {
            return endpoints.FirstOrDefault(endpoint => endpoint.ConnectionType == connectionType);
        }
    }
}
