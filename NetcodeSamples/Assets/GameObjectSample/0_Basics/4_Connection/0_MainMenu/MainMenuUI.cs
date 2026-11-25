using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace GameObjectSample
{
    public class MainMenuUI : MonoBehaviour
    {
        Button connectButton;
        Button hostButton;
        Button serverButton;
        TextField ipTextField;
        TextField portTextField;

        private VisualElement root;
        [SerializeField] NetcodeConfig.HostWorldMode m_HostWorldMode; // This is mostly for testing purposes. We only support single world for now.


        void OnEnable()
        {
            if (Netcode.IsServerBuildOnly)
            {
                StartServer(NetworkEndpoint.AnyIpv4.WithPort(7979));
                return;
            }
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            if (Netcode.IsServerBuildOnly)
                return;
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            if (connectButton != null)
            {
                connectButton.UnregisterCallback<ClickEvent>(ConnectButtonAction);
            }
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            root = rootElement;
            connectButton = (Button)root.Q("connect");
            hostButton = (Button)root.Q("host");
            serverButton = (Button)root.Q("serverListen");

            connectButton.RegisterCallback<ClickEvent>(ConnectButtonAction);
            hostButton.RegisterCallback<ClickEvent>(HostButtonAction);
            serverButton.RegisterCallback<ClickEvent>(ServerButtonAction);

            ipTextField = (TextField)root.Q("ip");
            ipTextField.value = "127.0.0.1";
            ipTextField.RegisterCallback<ChangeEvent<string>>(InputMessage);
            portTextField = (TextField)root.Q("port");
            portTextField.value = "7979";
        }

        void ServerButtonAction(ClickEvent evt)
        {
            root.SetEnabled(false);
            StartServer(NetworkEndpoint.Parse(ipTextField.text, ushort.Parse(portTextField.text)));
        }

        void StartServer(NetworkEndpoint listenEndpoint)
        {
            SceneManager.LoadScene("Game");
            Netcode.Listen(listenEndpoint);
        }

        void HostButtonAction(ClickEvent evt)
        {
            root.SetEnabled(false);
            SceneManager.LoadScene("Game");
            Netcode.StartAsHost(NetworkEndpoint.Parse(ipTextField.text, ushort.Parse(portTextField.text)), m_HostWorldMode);
        }

        private void ConnectButtonAction(ClickEvent evt)
        {
            root.SetEnabled(false);
            Netcode.OnConnectionEvent += SwitchSceneOnConnect;
            Netcode.Connect(NetworkEndpoint.Parse(ipTextField.text, ushort.Parse(portTextField.text)));
        }

        void SwitchSceneOnConnect(Connection connection, NetcodeConnectionEvent connectionEvent)
        {
            if (connectionEvent.State <= ConnectionState.State.Disconnected)
            {
                root.SetEnabled(true);
                Debug.LogError("Got error during connection "+connectionEvent.DisconnectReason);
                return;
            }

            if (connectionEvent.State != ConnectionState.State.Connected) return;
            Netcode.OnConnectionEvent -= SwitchSceneOnConnect;

            SceneManager.LoadScene("Game");
        }

        public static void InputMessage(ChangeEvent<string> evt)
        {
            Debug.Log($"{evt.newValue} -> {evt.target}");
        }
    }
}
