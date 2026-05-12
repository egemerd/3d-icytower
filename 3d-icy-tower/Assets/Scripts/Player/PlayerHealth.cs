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
        // PlatformGenerator henüz hazýr deðilse hata vermesin diye kontrol et
        if (PlatformGenerator.Instance == null) return;

        // Ölüm noktasýný doðrudan þu an sahnede olan en alt chunk'ýn alt tabanýndan (birleþim yerinden) al
        float deathLine = PlatformGenerator.Instance.GetDeathLineY();

        // Check if the player fell below the death line
        if (transform.position.y < deathLine)
        {
            GameOver();
        }
    }

    public float GetHealth()
    {
        return health;
    }
  
    public void GetDamage(int damage)
    {     
        health -= damage;
        TimeStop.Instance.StopTime(0.2f, 0.05f); 
        Debug.Log("Player Health: " + health);       
    }

    private void GameOver()
    {
        Debug.Log("Player fell past the active chunk limits! Game Over.");
        // Add your game over logic here (restart level, show UI, etc.)
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

    private void OnDrawGizmos()
    {
        if (PlatformGenerator.Instance == null || !Application.isPlaying) 
            return;

        // Çizimleri oluþtur
        float deathLineY = PlatformGenerator.Instance.GetDeathLineY();
        
        Vector3 playerPoint = transform.position;
        Vector3 deathPoint = new Vector3(transform.position.x, deathLineY, transform.position.z);

        // Draw player point (Green)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(playerPoint, 0.5f);

        // Draw chunk connection point / death line (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawCube(deathPoint, new Vector3(3f, 0.2f, 3f)); // Yerden oluþan düzlüðü görmek için bu sefer küp (ince bir plaka) çizdiriyorum. Ýstiyorsan tekrar DrawSphere yapabilirsin.

        // Draw thin line between them
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerPoint, deathPoint);
    }
}
