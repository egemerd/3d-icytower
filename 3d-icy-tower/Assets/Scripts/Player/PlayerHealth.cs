using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int health = 3;


    public float GetHealth()
    {
        return health;
    }
  
    public void GetDamage(int damage)
    {
        health -= damage;
        Debug.Log("Player Health: " + health);
    }


}
