using UnityEngine;
using System;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyPerPerfect = 25f;  // her perfect attack'te kazanýlan

    
    private float currentEnergy;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyPercent => currentEnergy / maxEnergy;

    // UI veya diðer sistemler buraya abone olur
    public event Action<float, float> OnEnergyChanged; // current, max
    public event Action OnEnergyFull;

    private void Awake()
    {
        currentEnergy = 0f;
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        Debug.Log($"Energy added: {amount}. Current Energy: {currentEnergy}/{maxEnergy}");
        if (currentEnergy >= maxEnergy)
            OnEnergyFull?.Invoke();
    }

    public bool TrySpendEnergy(float amount)
    {
        if (currentEnergy < amount) return false;

        currentEnergy = Mathf.Clamp(currentEnergy - amount, 0f, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        return true;
    }

    public void AddPerfectAttackEnergy()
    {
        AddEnergy(energyPerPerfect);
    }
}