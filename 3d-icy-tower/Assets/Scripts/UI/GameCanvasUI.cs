using UnityEngine;
using UnityEngine.UI;

public class GameCanvasUI : MonoBehaviour
{
    [SerializeField] private Image energyBar;
    [SerializeField] private Image healthBar;

    private PlayerHealth playerHealth;
    private EnergySystem energySystem;

    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        energySystem = FindObjectOfType<EnergySystem>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            // Baþlangýç deðerini set et
            UpdateHealthBar(playerHealth.health, playerHealth.maxHealth);
        }

        if (energySystem != null)
        {
            energySystem.OnEnergyChanged += UpdateEnergyBar;
            // Baþlangýç deðerini set et
            UpdateEnergyBar(energySystem.CurrentEnergy, energySystem.MaxEnergy);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealthBar;
        if (energySystem != null) energySystem.OnEnergyChanged -= UpdateEnergyBar;
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthBar != null)
            healthBar.fillAmount = (float)current / max;
    }

    private void UpdateEnergyBar(float current, float max)
    {
        Debug.Log($"Energy bar update: {current}/{max} = {current / max}");
        if (energyBar != null)
            energyBar.fillAmount = current / max;
    }
}