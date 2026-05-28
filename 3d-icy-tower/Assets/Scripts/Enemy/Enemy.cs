using UnityEngine;
using System.Collections;
using DG.Tweening;

public abstract class Enemy : MonoBehaviour, ITargetable
{
    [Header("Timing Attack Settings")]
    [SerializeField] private float timingWindowStart = 0.4f;
    [SerializeField] private float timingWindowEnd = 0.6f;
    [SerializeField] private float totalTimingDuration = 1f;

    [SerializeField] protected EnemyDataSO enemyData;

    [Header("Cursor Settings")]
    [SerializeField] private Transform[] cursors;
    [SerializeField] protected Transform cursorTarget;
    [SerializeField] private float cursorMoveDuration = 0.25f;
    [SerializeField] private GameObject uiPerfectSquare;
    [SerializeField] private GameObject perfectHitObj;
    [SerializeField] private SpriteRenderer perfectHitSprite;

    private Vector3[] defaultCursorPositions;

    public bool IsInTimingWindow { get; private set; }
    public bool IsInPerfectWindow { get; private set; }

    private Coroutine timingCoroutine;
    private Coroutine cursorMoveCoroutine;


    [Header("UI Visuals")]
    [SerializeField] private Transform timingUiTransform;

    protected Transform playerTransform;

    private bool isLockedOn = false;
    public bool useOldUiSystem = false;
    public bool isBoss;

    private float currentLockTimer = 0f;
    private float targetLockDelay = 1f;

    protected virtual void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        if (cursors != null)
        {
            defaultCursorPositions = new Vector3[cursors.Length];
            for (int i = 0; i < cursors.Length; i++)
            {
                defaultCursorPositions[i] = cursors[i].localPosition;
            }
        }
    }


    private void Start()
    {
        //StartCoroutine(TimingWindowCoroutine());
    }

    public Transform GetTransform()
    {
        return transform;
    }

    private void SetCursorActivation(bool active)
    {
        for (int i = 0; i < cursors.Length; i++) 
        {
            cursors[i].gameObject.SetActive(active);
        }
    }
    private void SetPerfectActive(bool active)
    {   
        uiPerfectSquare.SetActive(active);    
    }

    private void SetCursorColor(Color color)
    {
        for(int i = 0; i < cursors.Length; i++)
        {
            if (cursors[i].TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
            {
                sr.color = color;
            }
        }
    }

    public void OnKilled()
    {
        if(!isBoss)
        {
            Destroy(gameObject);
            Debug.Log("OnKilled");
        }
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
        //Debug.Log("Starting Timing UI");
        if (timingCoroutine != null) StopCoroutine(timingCoroutine);
        timingCoroutine = StartCoroutine(TimingWindowCoroutine());

        if (cursorMoveCoroutine != null) StopCoroutine(cursorMoveCoroutine);
        cursorMoveCoroutine = StartCoroutine(MoveCursorsToTarget());
    }

    // Call this if the player looks away or attacks successfully
    public void StopTimingUI()
    {
        IsInTimingWindow = false;
        if (timingUiTransform != null && useOldUiSystem) timingUiTransform.gameObject.SetActive(false);
        SetCursorActivation(false);
        //SetPerfectActive(false);

        if (timingCoroutine != null)
        {
            StopCoroutine(timingCoroutine);
            timingCoroutine = null;
        }
        if (cursorMoveCoroutine != null)
        {
            StopCoroutine(cursorMoveCoroutine);
            cursorMoveCoroutine = null;
        }

        

        if (cursors != null && defaultCursorPositions != null)
        {
            for (int i = 0; i < cursors.Length; i++)
            {
                if (cursors[i] != null && i < defaultCursorPositions.Length)
                {
                    cursors[i].localPosition = defaultCursorPositions[i];
                }
            }
        }
    }

   

    private IEnumerator MoveCursorsToTarget()
    {
        if (cursors == null || defaultCursorPositions == null || defaultCursorPositions.Length != cursors.Length)
        {
            yield break;
        }

        while (isLockedOn)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, cursorMoveDuration);

            // Baþlangýçta hepsini aktif et
            for (int i = 0; i < cursors.Length; i++)
            {
                if (cursors[i] != null)
                {
                    cursors[i].localPosition = defaultCursorPositions[i];
                    cursors[i].gameObject.SetActive(true);
                }
            }

            // ÇALIÞMA MANTIÐI:
            // CursorTarget'in pozisyonuna gitmek üzere "oran" hesapla.
            // Vector3.Lerp() ile "Local" uzayda target'ýn localine týrmanýyoruz.
            // Bu yöntem parent nereye giderse gitsin bozulmaz.

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < cursors.Length; i++)
                {
                    if (cursors[i] != null && cursorTarget != null)
                    {
                        // SADECE localPosition üzerinden LERP yapýyoruz.
                        // Çünkü Parent (Enemy) dünyada zaten yürüyor. Local'de biz de hedefe kayýyoruz.
                        cursors[i].localPosition = Vector3.Lerp(defaultCursorPositions[i], cursorTarget.localPosition, t);
                    }
                }

                yield return null;
            }

            // Süre tamamlanana (Target'ýn üstünde kalma aþamasý) kadar bekletme
            float remainingTime = totalTimingDuration - duration;
            float remainingElapsed = 0f;

            while (remainingElapsed < remainingTime)
            {
                remainingElapsed += Time.deltaTime;

                for (int i = 0; i < cursors.Length; i++)
                {
                    if (cursors[i] != null && cursorTarget != null)
                    {
                        cursors[i].localPosition = cursorTarget.localPosition;
                    }
                }

                yield return null;
            }
        }

        // Lock bitince/Kopunca Güvenlik Cleanup'ý
        for (int i = 0; i < cursors.Length; i++)
        {
            if (cursors[i] != null && i < defaultCursorPositions.Length)
            {
                cursors[i].localPosition = defaultCursorPositions[i];
                cursors[i].gameObject.SetActive(false);
            }
        }
    }
    private IEnumerator TimingWindowCoroutine()
    {
        if (timingUiTransform == null) yield break;
        SpriteRenderer renderer = timingUiTransform.GetComponent<SpriteRenderer>();

        // This replaces the old Update() check. It will loop indefinitely while locked on.
        while (isLockedOn)
        {
            timingUiTransform.gameObject.transform.position = transform.position;
            if (useOldUiSystem) timingUiTransform.gameObject.SetActive(true);

            SetCursorActivation(true);
            //SetPerfectActive(true);

            float elapsed = 0f;

            while (elapsed < totalTimingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / totalTimingDuration;

                if(useOldUiSystem)
                {
                    timingUiTransform.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one * 0.5f, t);
                }

                if (t >= timingWindowStart && t <= timingWindowEnd)
                {
                    IsInTimingWindow = true;
                    IsInPerfectWindow = true;
                    perfectHitSprite.color = Color.green;
                    SetCursorColor(Color.green);
                    if (renderer != null) renderer.color = Color.green;
                }
                else if (t <= timingWindowStart)
                {
                    IsInTimingWindow = false;
                    IsInPerfectWindow = false;
                    perfectHitSprite.color = Color.white;
                    SetCursorColor(Color.red);
                    if (renderer != null) renderer.color = Color.red;
                }
                else
                {
                    IsInTimingWindow = true;
                    IsInPerfectWindow = false;
                    perfectHitSprite.color = Color.white;
                    SetCursorColor(Color.yellow);
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
        if (useOldUiSystem) timingUiTransform.gameObject.SetActive(false);
        SetCursorActivation(false);
        //SetPerfectActive(false);
        timingCoroutine = null;
    }

    public void PerfectAttack()
    {
        Debug.Log("Perfect Attack!");
        Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

        // 1. Obje Instantiate edilir
        GameObject hitEffect = Instantiate(perfectHitObj, uiPerfectSquare.transform.position, rotation);

        // 2. Eðer üzerinde SpriteRenderer varsa direkt onun rengini solduralým (Fade)
        if (hitEffect.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            // Rengini alýp tam görünürden (alpha = 1) baþlatmak güvenlidir
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;

            // Sýfýr transparanlýða (alpha = 0) DOFade ile in. (Süre: 0.5 saniye)
            sr.DOFade(0f, 0.5f).SetEase(Ease.InQuad);
        }

        // 3. Orijinal pozisyonundan 1 birim kadar yukarý Smooth þekilde ilerlesin
        hitEffect.transform.DOMoveY(hitEffect.transform.position.y + 1f, 0.5f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                // Animasyon bitince sahnede yer kaplamasýn, sil
                Destroy(hitEffect);
            });
    }

    public abstract void EnemyAttack();
    public abstract void EnemyMovement();


}
