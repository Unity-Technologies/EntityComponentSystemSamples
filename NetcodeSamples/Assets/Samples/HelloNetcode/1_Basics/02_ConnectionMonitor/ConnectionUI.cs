using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    public class ConnectionUI : MonoBehaviour
    {
        public Canvas m_Canvas;
        public Button m_Button;
        public Text m_Text;

        private int m_VerticalSpace = 140;
        private int m_HorizontalSpace = -40;

        private void Update()
        {
            if (!ConnectionMonitorUIData.Connections.Data.IsCreated)
                return;
            while (ConnectionMonitorUIData.Connections.Data.TryDequeue(out var con))
            {
                if (con.ConnectionDeleted)
                {
                    GameObject buttonToDelete = GameObject.Find(con.Id.ToString());
                    if (buttonToDelete != null)
                    {
                        Destroy(buttonToDelete);
                    }
                }
                else
                {
                    var parent = new GameObject(con.Id.ToString());
                    parent.AddComponent<VerticalLayoutGroup>();
                    parent.transform.SetParent(m_Canvas.transform);
                    var buttonGo = Instantiate(m_Button, parent.transform, false);
                    buttonGo.GetComponent<RectTransform>().anchoredPosition3D += new Vector3(con.WorldIndex * m_VerticalSpace, (con.Id - 1) * m_HorizontalSpace, 0);
                    buttonGo.name = $"{con.WorldIndex} {con.Id}";
                    buttonGo.GetComponentInChildren<Text>().text = $"Disconnect {con.Id}";
                    buttonGo.onClick.AddListener(() => Disconnect(con));

                    var textGo = Instantiate(m_Text, parent.transform, false);
                    textGo.GetComponent<RectTransform>().anchoredPosition3D += new Vector3(con.WorldIndex * m_VerticalSpace, 0, 0);
                    textGo.name = $"{con.WorldIndex} {con.Id}";
                    textGo.text = con.World.Name.Value;
                }
            }
        }

        public void Disconnect(Connection con)
        {
            Debug.Log($"[{con.World.Name.Value}] Disconnecting {con.Id}");

            var connection = con.World.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamConnection>());
            var connectionIds = connection.ToComponentDataArray<NetworkId>(Allocator.Temp);
            var connectionEntities = connection.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connectionIds.Length; ++i)
            {
                if (connectionIds[i].Value == con.Id)
                    con.World.EntityManager.AddComponent<NetworkStreamRequestDisconnect>(connectionEntities[i]);
            }
        }
    }
}
