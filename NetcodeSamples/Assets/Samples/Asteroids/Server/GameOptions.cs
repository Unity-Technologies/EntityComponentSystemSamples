using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Asteroids.Host.UI
{
    public class GameOptions : MonoBehaviour
    {
        GameObject m_OptionsPanel;
        InputField m_AsteroidCountTextBox;
        EntityQuery m_ServerSettingsQuery;

        void Start()
        {
            m_OptionsPanel = gameObject.transform.Find("Panel").gameObject;
            m_OptionsPanel.SetActive(false);
            m_AsteroidCountTextBox = m_OptionsPanel.transform.Find("Count").GetComponent<InputField>();
        }

        void Update()
        {
            // Can only set options on the host/server
            if (ClientServerBootstrap.ServerWorld == null)
                return;

            if ( Input.GetKeyDown("o") )
            {
                m_OptionsPanel.SetActive(!m_OptionsPanel.activeSelf);

                if (m_OptionsPanel.activeSelf)
                {
                    if (m_ServerSettingsQuery==default)
                        m_ServerSettingsQuery = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntityQuery(typeof(ServerSettings));

                    if (m_ServerSettingsQuery.IsEmpty)
                        return;

                    var numAsteroids = m_ServerSettingsQuery.GetSingleton<ServerSettings>().levelData.numAsteroids;
                    m_AsteroidCountTextBox.text = $"{numAsteroids}";
                }
            }
        }

        public void SetAsteroidCount()
        {
            m_ServerSettingsQuery.GetSingletonRW<ServerSettings>().ValueRW.levelData.numAsteroids = Int32.Parse( m_AsteroidCountTextBox.text );
        }
    }
}
