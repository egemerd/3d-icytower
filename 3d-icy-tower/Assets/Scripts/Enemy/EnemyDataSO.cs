using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Rhythm Reticle UI")]
    public float reticleStartDistance = 2f;
    public float reticleEndDistance = 0.5f;

    
}
