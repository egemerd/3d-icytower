using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameCanvasUI : MonoBehaviour
{
    [SerializeField] private Image energyBar;
    [SerializeField] private Image healthBar;

    private PlayerHealth playerHealth;
    private EnergySystem energySystem;

    [SerializeField] private GameObject youDiedPanel;   // arka plan panel — sadece açýlýr
    [SerializeField] private GameObject youDiedText;  // "You Died" text'ini içeren panel
    [SerializeField] private float youDiedDelay = 2f;
    [SerializeField] private float youDiedAnimDuration = 0.6f;
    [SerializeField] private Ease youDiedEase = Ease.OutBack;

    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        energySystem = FindObjectOfType<EnergySystem>();

        youDiedPanel.SetActive(false);
        youDiedText.SetActive(false);
        youDiedText.transform.localScale = Vector3.zero;
        GameEvents.current.onGameOver += ShowYouDied;

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
        GameEvents.current.onGameOver -= ShowYouDied;
    }

    private void ShowYouDied()
    {
        if (youDiedPanel != null)
            youDiedPanel.SetActive(true);  // panel direkt açýlýr, animasyon yok

        if (youDiedText != null)
        {
            youDiedText.SetActive(true);
            youDiedText.transform.localScale = Vector3.zero; // küçükten baþla

            youDiedText.transform.DOScale(Vector3.one, youDiedAnimDuration)
                       .SetEase(youDiedEase);
        }
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