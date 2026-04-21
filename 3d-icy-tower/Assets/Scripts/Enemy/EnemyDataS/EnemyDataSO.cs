using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Rhythm Reticle UI")]
    public float reticleStartDistance = 2f;
    public float reticleEndDistance = 0.5f;

    [Header("Attack Settings")]
    public float scanRadius = 5f;
    public int attackDamage = 1;
    public float projectileSpeed = 1f;
    public float attackCooldown = 2f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

}
