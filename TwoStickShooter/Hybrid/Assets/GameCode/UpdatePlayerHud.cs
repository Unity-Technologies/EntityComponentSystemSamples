using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace TwoStickHybridExample
{
    [AlwaysUpdateSystem]
    public class UpdatePlayerHUD : ComponentSystem
    {
        public struct PlayerData
        {
            public int Length;
            public EntityArray Entity;
            public ComponentArray<PlayerInput> Input;
            public ComponentArray<Health> Health;
        }

        [Inject] PlayerData m_Players;

        private int m_CachedHealth = Int32.MinValue;

        public Button NewGameButton;
        public Text HealthText;

        public void SetupGameObjects()
        {
            NewGameButton = GameObject.Find("NewGameButton").GetComponent<Button>();
            HealthText = GameObject.Find("HealthText").GetComponent<Text>();
            NewGameButton.onClick.AddListener(TwoStickBootstrap.NewGame);
        }

        protected override void OnUpdate()
        {
            if (m_Players.Length > 0)
            {
                UpdateAlive();
            }
            else
            {
                UpdateDead();
            }
        }

        private void UpdateDead()
        {
            if (HealthText != null)
            {
                HealthText.gameObject.SetActive(false);
            }
            if (NewGameButton != null)
            {
                NewGameButton.gameObject.SetActive(true);
            }
        }

        private void UpdateAlive()
        {
            HealthText.gameObject.SetActive(true);
            NewGameButton.gameObject.SetActive(false);

            int displayedHealth = (int)m_Players.Health[0].Value;

            if (m_CachedHealth != displayedHealth)
            {
                HealthText.text = $"HEALTH: {displayedHealth}";
                m_CachedHealth = displayedHealth;
            }
        }
    }
}