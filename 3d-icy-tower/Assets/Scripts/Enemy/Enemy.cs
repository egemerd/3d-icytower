using UnityEngine;

public abstract class Enemy : MonoBehaviour, ITargetable
{
    [SerializeField] protected EnemyDataSO enemyData;

    [Header("UI Visuals")]
    [SerializeField] private Transform timingUiTransform;

    protected Transform playerTransform;

    private bool isLockedOn = false;

    private float currentLockTimer = 0f;
    private float targetLockDelay = 1f;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

 

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnKilled()
    {
        Debug.Log("OnKilled");
    }

    

    public void OnLockOff()
    {
        Debug.Log("Enemy lock lost!");
        isLockedOn = false;
        currentLockTimer = 0f;       
    }

    public void OnLockOn(float lockOnDelay)
    {
        Debug.Log("Enemy locked on!");
        isLockedOn = true;
        currentLockTimer = 0f;
        targetLockDelay = lockOnDelay;       
    }

    public abstract void EnemyAttack();
    public abstract void EnemyMovement();


}
