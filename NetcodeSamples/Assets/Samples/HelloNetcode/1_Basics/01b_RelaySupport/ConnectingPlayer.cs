using System;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Responsible for joining relay server using join code retrieved from <see cref="HostServer"/>.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ConnectingPlayer : SystemBase
    {
        Task<JoinAllocation> m_JoinTask;
        Task m_SetupTask;
        ClientStatus m_ClientStatus;
        string m_RelayJoinCode;
        NetworkEndpoint m_Endpoint;
        NetworkConnection m_ClientConnection;
        public RelayServerData RelayClientData;
        public RelayFrontend UIBehaviour;

        [Flags]
        enum ClientStatus
        {
            Unknown,
            FailedToConnect,
            Ready,
            GetJoinCodeFromHost,
            WaitForJoin,
            WaitForInit,
            WaitForSignIn,
        }

        protected override void OnCreate()
        {
            RequireForUpdate<EnableRelayServer>();
            m_ClientStatus = ClientStatus.Unknown;
        }

        public void GetJoinCodeFromHost()
        {
            m_ClientStatus = ClientStatus.GetJoinCodeFromHost;
        }

        public void JoinUsingCode(string joinCode)
        {
#if !UNITY_SERVER
            UIBehaviour.ClientConnectionStatus = "Waiting for relay response";
#endif
            m_RelayJoinCode = joinCode;
            m_SetupTask = UnityServices.InitializeAsync();
            m_ClientStatus = ClientStatus.WaitForInit;
        }

        protected override void OnUpdate()
        {
            switch (m_ClientStatus)
            {
                case ClientStatus.Ready:
                {
#if !UNITY_SERVER
                    UIBehaviour.ClientConnectionStatus = "Success";
#endif
                    m_ClientStatus = ClientStatus.Unknown;
                    return;
                }
                case ClientStatus.FailedToConnect:
                {
#if !UNITY_SERVER
                    UIBehaviour.ClientConnectionStatus = "Failed, check console";
#endif
                    m_ClientStatus = ClientStatus.Unknown;
                    return;
                }
                case ClientStatus.GetJoinCodeFromHost:
                {
#if !UNITY_SERVER
                    UIBehaviour.ClientConnectionStatus = "Waiting for join code from host server";
#endif
                    var hostServer = World.GetExistingSystemManaged<HostServer>();
                    m_ClientStatus = JoinUsingJoinCode(hostServer.JoinCode, out m_JoinTask);
                    return;
                }
                case ClientStatus.WaitForJoin:
                {
#if !UNITY_SERVER
                    UIBehaviour.ClientConnectionStatus = "Binding to relay server";
#endif
                    m_ClientStatus = WaitForJoin(m_JoinTask, out RelayClientData);
                    return;
                }
                case ClientStatus.WaitForInit:
                {
                    if (m_SetupTask.IsCompleted)
                    {
                        if (!AuthenticationService.Instance.IsSignedIn)
                        {
                            m_SetupTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
                            m_ClientStatus = ClientStatus.WaitForSignIn;
                        }
                    }
                    return;
                }
                case ClientStatus.WaitForSignIn:
                {
                    if (m_SetupTask.IsCompleted)
                        m_ClientStatus = JoinUsingJoinCode(m_RelayJoinCode, out m_JoinTask);
                    return;
                }
                case ClientStatus.Unknown:
                default:
                    break;
            }
        }

        static ClientStatus WaitForJoin(Task<JoinAllocation> joinTask, out RelayServerData relayClientData)
        {
            if (!joinTask.IsCompleted)
            {
                relayClientData = default;
                return ClientStatus.WaitForJoin;
            }

            if (joinTask.IsFaulted)
            {
                relayClientData = default;
                Debug.LogError("Join Relay request failed");
                Debug.LogException(joinTask.Exception);
                return ClientStatus.FailedToConnect;
            }

            return BindToRelay(joinTask, out relayClientData);
        }

        static ClientStatus BindToRelay(Task<JoinAllocation> joinTask, out RelayServerData relayClientData)
        {
            // Collect and convert the Relay data from the join response
            var allocation = joinTask.Result;

            // Format the server data, based on desired connectionType
            try
            {
                relayClientData = PlayerRelayData(allocation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                relayClientData = default;
                return ClientStatus.FailedToConnect;
            }

            return ClientStatus.Ready;
        }

        static ClientStatus JoinUsingJoinCode(string hostServerJoinCode, out Task<JoinAllocation> joinTask)
        {
            if (hostServerJoinCode == null)
            {
                joinTask = null;
                return ClientStatus.GetJoinCodeFromHost;
            }

            // Send the join request to the Relay service
            joinTask = RelayService.Instance.JoinAllocationAsync(hostServerJoinCode);
            return ClientStatus.WaitForJoin;
        }

        static RelayServerData PlayerRelayData(JoinAllocation allocation, string connectionType = "dtls")
        {
            // Select endpoint based on desired connectionType
            var endpoint = RelayUtilities.GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
            if (endpoint == null)
            {
                throw new Exception($"endpoint for connectionType {connectionType} not found");
            }

            // Prepare the server endpoint using the Relay server IP and port
            var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
            var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
            var hostConnectionData = RelayConnectionData.FromByteArray(allocation.HostConnectionData);
            var key = RelayHMACKey.FromByteArray(allocation.Key);

            // Prepare the Relay server data and compute the nonce values
            // A player joining the host passes its own connectionData as well as the host's
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref hostConnectionData, ref key, connectionType == "dtls");

            return relayServerData;
        }
    }
}
