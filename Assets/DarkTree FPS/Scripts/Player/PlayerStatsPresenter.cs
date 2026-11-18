using UnityEngine;
using UnityEngine.UI;

namespace DarkTreeFPS
{
    /// <summary>
    /// Presentation adapter for player vital-stat UI. Gameplay state remains owned by PlayerStats.
    /// Existing PlayerStats UI fields are used as a migration bridge until the UI prefab is separated.
    /// </summary>
    public sealed class PlayerStatsPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerStats source;
        [SerializeField] private Text healthText = null;
        [SerializeField] private Slider healthSlider = null;
        [SerializeField] private Text staminaText = null;
        [SerializeField] private Slider staminaSlider = null;
        [SerializeField] private Text hungerText = null;
        [SerializeField] private Slider hungerSlider = null;
        [SerializeField] private Text thirstText = null;
        [SerializeField] private Slider thirstSlider = null;

        private void Reset()
        {
            source = GetComponent<PlayerStats>();
        }

        private void Awake()
        {
            if (source == null)
                source = GetComponent<PlayerStats>();
        }

        public void Present(int health, float stamina, int hunger, int thirst)
        {
            if (healthText != null) healthText.text = health.ToString();
            if (healthSlider != null) healthSlider.value = health;
            if (staminaText != null) staminaText.text = ((int)stamina).ToString();
            if (staminaSlider != null) staminaSlider.value = (int)stamina;
            if (hungerText != null) hungerText.text = hunger.ToString();
            if (hungerSlider != null) hungerSlider.value = hunger;
            if (thirstText != null) thirstText.text = thirst.ToString();
            if (thirstSlider != null) thirstSlider.value = thirst;
        }
    }
}
