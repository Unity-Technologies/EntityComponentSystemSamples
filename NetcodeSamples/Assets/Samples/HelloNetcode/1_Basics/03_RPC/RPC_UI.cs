using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    public class RpcUi : MonoBehaviour
    {
        public InputField m_InputText;
        public Text m_ChatText;
        public Canvas m_Canvas;
        public Text m_UserPrefab;
        public RectTransform m_ChatContent;
        public ScrollRect m_ScrollRect;
        public GameObject m_ChatWindowPrefab;

        private int m_CurrentUserSlot = 0;
        private int m_UserSlotHorizontalSpace = -13;
        private int m_OwnUser = -1;
        List<Text> m_CachedUserTexts;

        void Start()
        {
            m_CachedUserTexts = m_Canvas.GetComponentsInChildren<Text>().ToList();
        }

        void Update()
        {
            if (m_OwnUser == -1 && ClientServerBootstrap.ClientWorld != null)
            {
                // Non-thin client will always be first in the list
                var connectionQuery = ClientServerBootstrap.ClientWorld.EntityManager
                    .CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                var connectionIds = connectionQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
                if (connectionIds.Length > 0)
                {
                    // Client only has one connection
                    m_OwnUser = connectionIds[0].Value;
                }
            }

            if (RpcUiData.Messages.Data.IsCreated && RpcUiData.Messages.Data.TryDequeue(out var message))
            {
                var chatText = Instantiate(m_ChatText, m_ChatContent.transform);
                // Color the message blue in case this is our own message
                if (message.ConvertToString().StartsWith($"User {m_OwnUser}"))
                    chatText.text += $"<color=blue>{message}</color>\n";
                else
                    chatText.text += $"{message}\n";

                // Scroll the chat text to the bottom so you see latest message
                // (when text does not fit any more in the content space)
                Canvas.ForceUpdateCanvases();
                m_ScrollRect.verticalNormalizedPosition = 0f;
            }

            if (RpcUiData.Users.Data.IsCreated && RpcUiData.Users.Data.TryDequeue(out var user))
            {
                var userText = GetUserText();
                userText.text = $"User {user}";
                m_CurrentUserSlot++;

                // Color the name blue in case this is our own user
                if (user == m_OwnUser)
                    userText.color = Color.blue;
            }
        }

        Text GetUserText()
        {
            foreach (var text in m_CachedUserTexts)
            {
                if (!text.enabled)
                {
                    text.enabled = true;
                    return text;
                }
            }
            var newText = Instantiate(m_UserPrefab, m_Canvas.transform, false);
            newText.GetComponent<RectTransform>().anchoredPosition3D += new Vector3(0,m_CurrentUserSlot*m_UserSlotHorizontalSpace, 0);
            m_CachedUserTexts.Add(newText);
            return newText;
        }

        public void SendChatMessage()
        {
            SendRPC(ClientServerBootstrap.ClientWorld, m_InputText.text);
            // Clear the input text as the message has been sent, and place the UI focus back on it
            // so it's ready to accept the next message
            m_InputText.text = "";
            m_InputText.Select();
            m_InputText.ActivateInputField();
        }

        void SendRPC(World world, string message, Entity targetEntity = default)
        {
            if (world == null || !world.IsCreated) return;
            var entity = world.EntityManager.CreateEntity();
            world.EntityManager.AddComponentData(entity, new ChatMessage() { Message = message});
            world.EntityManager.AddComponent<SendRpcCommandRequest>(entity);
            if (targetEntity != Entity.Null)
                world.EntityManager.SetComponentData(entity,
                    new SendRpcCommandRequest() { TargetConnection = targetEntity });
        }
    }
}
