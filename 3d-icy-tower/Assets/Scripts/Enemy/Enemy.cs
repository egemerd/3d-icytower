using UnityEngine;
using System.Collections;

public abstract class Enemy : MonoBehaviour, ITargetable
{
    [Header("Timing Attack Settings")]
    [SerializeField] private float timingWindowStart = 0.4f;
    [SerializeField] private float timingWindowEnd = 0.6f;
    [SerializeField] private float totalTimingDuration = 1f;

    [SerializeField] protected EnemyDataSO enemyData;
    public bool IsInTimingWindow { get; private set; }
    private Coroutine timingCoroutine;

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

    private void Update()
    {
        Debug.Log(isLockedOn);

    }
    private void Start()
    {
        StartCoroutine(TimingWindowCoroutine());
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnKilled()
    {
        Destroy(gameObject);
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

    public void StartTimingUI()
    {
        Debug.Log("Starting Timing UI");
        if (timingCoroutine != null) StopCoroutine(timingCoroutine);
        timingCoroutine = StartCoroutine(TimingWindowCoroutine());
    }

    // Call this if the player looks away or attacks successfully
    public void StopTimingUI()
    {
        if (timingCoroutine != null)
        {
            StopCoroutine(timingCoroutine);
            timingCoroutine = null;
        }
        IsInTimingWindow = false;
        if (timingUiTransform != null) timingUiTransform.gameObject.SetActive(false);
    }

    private IEnumerator TimingWindowCoroutine()
    {
        if (timingUiTransform == null) yield break;
        SpriteRenderer renderer = timingUiTransform.GetComponent<SpriteRenderer>();

        // This replaces the old Update() check. It will loop indefinitely while locked on.
        while (isLockedOn)
        {
            timingUiTransform.gameObject.transform.position = transform.position;
            timingUiTransform.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < totalTimingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / totalTimingDuration;

                // Update UI visual (shrinking local scale)
                timingUiTransform.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one * 0.5f, t);

                if (t >= timingWindowStart && t <= timingWindowEnd)
                {
                    IsInTimingWindow = true;
                    if (renderer != null) renderer.color = Color.green;
                }
                else
                {
                    IsInTimingWindow = false;
                    if (renderer != null) renderer.color = Color.yellow;
                }

                yield return null;
            }

            // At the end of the 1-second pulse, reset it and immediately loop again
            IsInTimingWindow = false;
            if (renderer != null) renderer.color = Color.yellow;

            // Note: If you want a small delay between pulses, you could add:
            // yield return new WaitForSeconds(0.2f);
        }

        // If the enemy is no longer locked on, shut down the UI
        timingUiTransform.gameObject.SetActive(false);
        timingCoroutine = null;
    }

    public abstract void EnemyAttack();
    public abstract void EnemyMovement();


}
