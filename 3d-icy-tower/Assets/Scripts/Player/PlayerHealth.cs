using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int health = 3;
    public bool canTakeDamage = true;

    [SerializeField] private int guiFontSize = 48;
    [SerializeField] private float topPadding = 20f;
    [SerializeField] private float rightPadding = 30f;

    private GUIStyle healthStyle;


    private void Update()
    {
        Debug.Log("Can Take Damage: " + canTakeDamage);
    }
    public float GetHealth()
    {
        return health;
    }
  
    public void GetDamage(int damage)
    {
        if (canTakeDamage)
        {
            health -= damage;
            Debug.Log("Player Health: " + health);
        }
    }

    private void OnGUI()
    {
        if (healthStyle == null)
        {
            healthStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = guiFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            };
            healthStyle.normal.textColor = Color.white;
        }

        Rect rect = new Rect(0f, topPadding, Screen.width - rightPadding, 80f);
        GUI.Label(rect, $"HEALTH: {health}", healthStyle);
    }
}
