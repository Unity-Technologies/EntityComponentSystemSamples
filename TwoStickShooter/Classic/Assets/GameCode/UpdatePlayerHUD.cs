using UnityEngine;
using UnityEngine.UI;

namespace TwoStickClassicExample
{
    public class UpdatePlayerHUD : MonoBehaviour
    {
        private float m_CachedHealth;

        public Button NewGameButton;
        public Text HealthText;

        private void Start()
        {
            NewGameButton.onClick.AddListener(TwoStickBootstrap.NewGame);
        }

        private void Update()
        {
            var player = FindObjectOfType<Player>();
            if (player != null)
            {
                UpdateAlive(player);
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

        private void UpdateAlive(Player player)
        {
            HealthText.gameObject.SetActive(true);
            NewGameButton.gameObject.SetActive(false);
            
            var displayedHealth = 0;
            if (player != null)
            {
                displayedHealth = (int) player.GetComponent<Health>().Value;
            }

            if (m_CachedHealth != displayedHealth)
            {
                if (displayedHealth > 0)
                    HealthText.text = $"HEALTH: {displayedHealth}";
                else
                    HealthText.text = "GAME OVER";
                m_CachedHealth = displayedHealth;
            }
        }
    }
}