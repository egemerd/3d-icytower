using UnityEngine;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private Boss boss;

    [SerializeField] private GameObject healthBarUI;

    private void Start()
    {
        GameEvents.current.onBossGetDamage += UpdateBossBar;
       
    }

    private void OnDisable()
    {
        GameEvents.current.onBossGetDamage -= UpdateBossBar;
    }


    private void UpdateBossBar()
    {
        if (boss == null || healthBarUI == null) return;
        // Boss'un mevcut HP'sini al
        float currentHp = boss.GetCurrentHp(); // Boss sýnýfýnda GetCurrentHp() metodu olmalý
        float maxHp = boss.GetMaxHp(); // Boss sýnýfýnda GetMaxHp() metodu olmalý
        // Health barýný güncelle
        float fillAmount = currentHp / maxHp;
        healthBarUI.GetComponent<UnityEngine.UI.Image>().fillAmount = fillAmount;
    }
}
