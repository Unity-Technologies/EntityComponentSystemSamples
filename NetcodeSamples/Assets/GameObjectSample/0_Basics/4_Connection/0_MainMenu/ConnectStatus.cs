using System;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameObjectSample
{
    public class ConnectStatus : MonoBehaviour
    {
        Label m_StatusText;
        void OnEnable()
        {
            if (Netcode.IsServerBuildOnly)
            {
                return;
            }

            Netcode.OnConnectionEvent += OnConnectionEvent;
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            if (Netcode.IsServerBuildOnly)
            {
                return;
            }

            Netcode.OnConnectionEvent -= OnConnectionEvent;
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
        }

        void OnConnectionEvent(Connection connection, NetcodeConnectionEvent connectionEvent)
        {
            if (connection != Netcode.LocalConnection)
                return;
            m_StatusText.text = connectionEvent.State.ToString();
            if (connectionEvent.State == ConnectionState.State.Disconnected)
                m_StatusText.text += "\nReason: "+connectionEvent.DisconnectReason.ToString();
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            m_StatusText = (UnityEngine.UIElements.Label)rootElement.Q("status");
            m_StatusText.text = Netcode.LocalConnection.GetConnectionState().ToString();

        }
    }
}
