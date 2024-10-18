using System;
using System.Collections;
using Unity.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Samples.HelloNetcode
{
    public class PlayerAuthentication : MonoBehaviour
    {
        void Start()
        {
            if (!ConnectionApprovalData.PlayerAuthenticationEnabled.Data)
                return;

            // We'll only reach here on client worlds with auth enabled so this will never be done on servers
            SignIn();
        }

        async void SignIn()
        {
            try
            {
                await UnityServices.InitializeAsync();

                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log("Sign in anonymously succeeded!");

                    ConnectionApprovalData.ApprovalPayload.Data.Payload.Append(AuthenticationService.Instance.PlayerId);
                    ConnectionApprovalData.ApprovalPayload.Data.Payload.Append(':');
                    ConnectionApprovalData.ApprovalPayload.Data.Payload.Append(AuthenticationService.Instance
                        .AccessToken);
                };

                AuthenticationService.Instance.SignInFailed += errorResponse =>
                {
                    Debug.LogError($"Sign in anonymously failed with error code: {errorResponse.ErrorCode}");
                };

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception when trying to sign in: {e.Message}");
            }
        }

        IEnumerator GetPlayerInfo(PendingApproval pendingApproval)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://player-auth.services.api.unity.com/v1/users/{pendingApproval.PlayerId}"))
            {
                webRequest.SetRequestHeader("ProjectId", Application.cloudProjectId);
                webRequest.SetRequestHeader("Authorization", $"Bearer {pendingApproval.AccessToken}");

                yield return webRequest.SendWebRequest();

                bool success = false;
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError($"GetPlayerInfo ConnectionError: {webRequest.error}");
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError($"GetPlayerInfo DataProcessingError: {webRequest.error}");
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError($"GetPlayerInfo ProtocolError: {webRequest.error}");
                        break;
                    case UnityWebRequest.Result.Success:
                        success = true;
                        break;
                }

                ConnectionApprovalData.ApprovalResults.Data.Enqueue(new ApprovalResult(){ Success = success, Payload = pendingApproval.Payload, ConnectionEntity = pendingApproval.ConnectionEntity});
            }
        }

        void Update()
        {
            // NOTE: This will fetch all queued player approval requests but will only work for up to 15 players at a time as then
            // this particular service rate limit will be reached. Would need to fetch the rest in batches after limit is cleared.
            // See https://services.docs.unity.com/player-auth/v1/ for more information.
            while (ConnectionApprovalData.PendingApprovals.Data.IsCreated && ConnectionApprovalData.PendingApprovals.Data.TryDequeue(out var pendingApproval))
            {
                StartCoroutine(GetPlayerInfo(pendingApproval));
            }
        }
    }
}
