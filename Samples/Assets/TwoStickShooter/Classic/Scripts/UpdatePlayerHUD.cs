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
            if (Player.Current != null)
            {
                UpdateAlive(Player.Current);
            }
            else
            {
                UpdateDead();
            }
        }

        private void UpdateDead()
        {
            HealthText.gameObject.SetActive(false);
            NewGameButton.gameObject.SetActive(true);
        }

        private void UpdateAlive(Player player)
        {
            HealthText.gameObject.SetActive(true);
            NewGameButton.gameObject.SetActive(false);
            
            var displayedHealth = (int) player.GetComponent<Health>().Value;

            if (m_CachedHealth != displayedHealth)
            {
                HealthText.text = displayedHealth > 0 ? $"HEALTH: {displayedHealth}" : "GAME OVER";
                m_CachedHealth = displayedHealth;
            }
        }
    }
}