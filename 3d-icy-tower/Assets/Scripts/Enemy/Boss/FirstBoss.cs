using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
public class FirstBoss : Boss
{
    private enum FirstBossState { Idle, Bounce, Dash , Death}
    private FirstBossState currentState;

    [Header("Weighted Random (Attacks only)")]
    [SerializeField] private float weightBounce = 2f;
    [SerializeField] private float weightDash = 1.5f;

    [Header("Idle Settings")]
    [SerializeField] private float idleDuration = 2f;
    private float idleTimer;

    [Header("Bounce Settings")]
    [SerializeField] private float bounceSpeed = 8f;
    [SerializeField] private float minBounceAngle = 20f;    // En dik (yukarıya) açı
    [SerializeField] private float maxBounceAngle = 60f;    // En yatay açı
    [SerializeField] private int maxGroundBounces = 2;      // End state after hitting the ground this many times
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float bounceRadius = 0.5f;

    private int currentGroundBounces = 0;
    private Vector3 bounceDirection;
    private bool isBouncing = false;
    private bool bounceHit;

    [Header("Dash Settings")]
    [SerializeField] private float dashDuration = 0.8f;      // Toplam fırlama süresi
    [SerializeField] private float dashDamage = 20f;
    [SerializeField] private float pastPlayerMinDist = 2f;   // Player'ı minimum bu kadar geçsin
    [SerializeField] private float pastPlayerMaxDist = 6f;   // Player'ı maksimum bu kadar geçsin
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine dashCoroutine;
    private bool dashHit;

    [Header("State Change Logic")]
    [SerializeField] private float maxVertical = 4f;
    [SerializeField] private float minHorizantal = 10f;

    [Header("Boss Detection")]
    [SerializeField] private float coneAngle = 100f;

    [Header("Plater Attack Offset")]
    [SerializeField] private Transform targetPoint; // boş child obje
    [SerializeField] private float offsetAmount = 1.5f;

    [Header("Vulnerability Settings")]
    [SerializeField] private float minVulnDuration = 1f;    // En az kaç sn Vurulabilir kalsın?
    [SerializeField] private float maxVulnDuration = 3f;    // En çok kaç sn Vurulabilir kalsın?
    [SerializeField] private float minImmuneDuration = 1f;  // En az kaç sn Vurulamaz (Zırhlı) kalsın?
    [SerializeField] private float maxImmuneDuration = 2f;  // En çok kaç sn Vurulamaz (Zırhlı) kalsın?

    [Header("Weak Point Indicator (Vulnerable Visual)")]
    // Boss'un kafasına ekleyeceğiniz çocuk(child) objeyi buraya sürükleyin
    [SerializeField] private Transform weakPointObj;

    [Header("Boss Damage Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float timeStopScale = 0f;
    [SerializeField] private float timeStopDuration = 0.3f;

    [Header("HeadBump Settings")]
    [SerializeField] private float hiddenYPos = -0.5f;
    [SerializeField] private float exposedYPos = 1.0f;
    [SerializeField] private float exposeAnimationDuration = 0.5f;

    [Header("Boss Hit VFX")] 
    [SerializeField] private VisualEffect bossHitVFX;

    [Header("Boss Death VFX")]
    [SerializeField] private VisualEffect bossDeathVFX;
    [SerializeField] private int vfxSpawnCount = 4;
    [SerializeField] private float vfxMinInterval = 0.1f;
    [SerializeField] private float vfxMaxInterval = 0.4f;
    [SerializeField] private float shakeVfxInterval = 0.15f;
    [SerializeField] private float shrinkDuration = 1f;
    [SerializeField] private Ease shrinkEase = Ease.InBack;
    private float vfxTimer = 0f;


    private Coroutine vulnerabilityCoroutine;
    protected bool IsVulnerable { get; private set; }

    private Rigidbody rb;

    private Vector3 gizmoBounceDir;
    private Vector3 gizmoDashDir;

    private Vector3 originalScale;

    protected override void Start()
    {
        base.Start();
        originalScale = transform.localScale;
        GameEvents.current.onBossDead += TriggerFirstBossDeath;
    }


    private void OnDestroy()
    {

        GameEvents.current.onBossDead -= TriggerFirstBossDeath;
    }

    protected override void OnBossStart()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        bossHitVFX.Stop();
        if (weakPointObj != null)
        {
            Vector3 startLocal = weakPointObj.localPosition;
            startLocal.y = hiddenYPos;
            weakPointObj.localPosition = startLocal;

            // İsterseniz görünmez de yapabilirsiniz ama gömülü olması yetiyorsa kapamayın.
            // weakPointObj.gameObject.SetActive(false); 
        }

        vulnerabilityCoroutine = StartCoroutine(VulnerabilityRoutine());

        EnterState(FirstBossState.Idle);
    }

    protected override void RunStateMachine()
    {
        switch (currentState)
        {
            case FirstBossState.Idle: StateIdle(); break;
            case FirstBossState.Bounce: StateBounce(); break;
            case FirstBossState.Dash: StateDash(); break;
            case FirstBossState.Death: break; 
        }
    }

    public override void OnKilled(int damage)
    {
        if (IsVulnerable && CanBeAttackedFrom(playerTransform.position))
        {
            TakeDamage(damage);
            bossHitVFX.transform.position = transform.position + new Vector3(0, 1f, 0); // VFX'i boss'un kafasının biraz üstünde oynat
            bossHitVFX.Play();
            Debug.Log("[FirstBoss] Attack yapıldı!");
        }
        else
        {
            Debug.Log("[FirstBoss] Vurulmaya çalışıldı ama saldırı etkisizdi! (Vulnerable: " + IsVulnerable + ", CanBeAttacked: " + CanBeAttackedFrom(playerTransform.position) + ")");
        }
    }

    public override bool CanBeAttackedFrom(Vector3 attackerPosition)
    {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.up, directionToPlayer);

        if (angle < coneAngle / 2f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override Transform GetTransform()
    {
        if (currentState == FirstBossState.Bounce)
        {
            //Debug.Log("bounce attack, no offset");
            return transform;
        }
        else
        {
            //Debug.Log("attack with offset");
            Vector3 offset = (playerTransform.position - transform.position).normalized * offsetAmount;
            targetPoint.position = transform.position + offset;

            return targetPoint;
        }
    }

    // ── STATE GEÇİŞ ────────────────────────────────────────────
    private void EnterState(FirstBossState newState)
    {
        // Önceki state temizliği
        rb.linearVelocity = Vector3.zero;

        currentState = newState;
        Debug.Log($"[FirstBoss] State → <color=cyan>{newState}</color>  |  Phase: {currentPhase}");

        switch (newState)
        {
            case FirstBossState.Idle:
                idleTimer = idleDuration;
                break;

            case FirstBossState.Bounce:             
                currentGroundBounces = 0;

                // 1. Min ve Max açılar arasından rastgele bir fırlama açısı (yukarıdan sapma payı) seç
                float randomAngle = Random.Range(minBounceAngle, maxBounceAngle);

                // 2. Oyuncunun konumuna göre Yön bul (Sağa mı sola mı?)
                // Eğer oyuncu benden daha ileri Z konumundaysa, sağa (Z pozitif) doğru uçmalıyım.
                // Y=Cos(angle) points up, Z=Sin(angle) points forward/backward
                if (playerTransform != null && playerTransform.position.z < transform.position.z)
                {
                    // Oyuncu boss'un solundaysa / arkasındaysa, açıyı negatife çevir (Sola -Z'ye zıpla)
                    randomAngle = -randomAngle;
                }

                bounceDirection = new Vector3(
                    0f,
                    Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                    Mathf.Sin(randomAngle * Mathf.Deg2Rad)
                ).normalized;

                gizmoBounceDir = bounceDirection;
                rb.linearVelocity = bounceDirection * bounceSpeed;
                break;

            case FirstBossState.Dash:
                dashHit = false;
                rb.linearVelocity = Vector3.zero;

                if (playerTransform == null)
                {
                    EnterState(FirstBossState.Idle);
                    break;
                }

                // 2.5D için yön tayini (Sadece Z ekseninde ileri veya geri)
                float startZ = transform.position.z;
                float playerZ = playerTransform.position.z;
                float directionToPlayer = Mathf.Sign(playerZ - startZ);
                Vector3 dashDir = new Vector3(0f, 0f, directionToPlayer);

                // Default olarak oyuncunun epeyce arkasını hedef alıyoruz
                float extraDistance = Random.Range(pastPlayerMinDist, pastPlayerMaxDist);
                float desiredTargetZ = playerZ + (directionToPlayer * extraDistance);

                // --- DUVAR KONTROLÜ (Raycast) ---
                // Boss'tan dümdüz oyuncu yönüne doğru sonsuz ışın atalım ki arkasındaki duvarı bulalım
                float maxAllowedZ = desiredTargetZ; // Şimdilik varsayılan hedef

                if (Physics.Raycast(transform.position, dashDir, out RaycastHit hit, 100f, wallMask))
                {
                    // Duvarın Z koordinatı
                    float wallZ = hit.point.z;

                    // Bossun içine girmemesi için Duvar'dan "bounceRadius" (kendi genişliğimiz) kadar GERİ çekiliyoruz
                    float safeWallZ = wallZ - (directionToPlayer * (bounceRadius + 0.1f));

                    // Acaba istediğimiz hedef, duvarı aşıyor mu?
                    // Pozitif z yönündeysek (İleri gidiyorsak):
                    if (directionToPlayer > 0 && desiredTargetZ > safeWallZ)
                    {
                        maxAllowedZ = safeWallZ;
                    }
                    // Negatif z yönündeysek (Geri gidiyorsak):
                    else if (directionToPlayer < 0 && desiredTargetZ < safeWallZ)
                    {
                        maxAllowedZ = safeWallZ;
                    }
                    else
                    {
                        // İsteğimiz zaten duvarın içindeyse ve player ile duvar arasındaysa onu kullan
                        maxAllowedZ = desiredTargetZ;
                    }
                }
                else
                {
                    maxAllowedZ = desiredTargetZ; // Duvar bulunamazsa direkt atadığımız son noktaya uç
                }

                // Elde edilen güvenli Hedef Noktası (Y'si uçmamak için sabit tutulur)
                Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, maxAllowedZ);
                gizmoDashDir = (targetPos - transform.position).normalized;

                // Eski Update/FixedUpdate akışını bypass edip pürüzsüz coroutine ile fırlatma başlat
                dashCoroutine = StartCoroutine(DashRoutine(transform.position, targetPos));
                break;
        }
    }

    private IEnumerator VulnerabilityRoutine()
    {
        while (true)
        {
            // -----------------------------------------------------
            // 1. Durum: VURULABİLİR YAPIYORUZ (Zırh kırıldı / Çıktı)
            // -----------------------------------------------------
            IsVulnerable = true;
            float vulnTime = Random.Range(minVulnDuration, maxVulnDuration);
            Debug.Log($"[FirstBoss] Boss is VULNERABLE for {vulnTime:F1} seconds!");

            // Objeyi yukarıya doğru çıkart (Animasyonlu)
            if (weakPointObj != null)
            {
                // weakPointObj.gameObject.SetActive(true); // Görünmez ise görünür yap
                weakPointObj.DOLocalMoveY(exposedYPos, exposeAnimationDuration).SetEase(Ease.OutBack);

                // Opsiyonel: Çıkarken yeşil/kırmızı oynamak isterseniz (Eğer material'i varsa)
                // weakPointObj.GetComponent<MeshRenderer>().material.DOColor(Color.red, exposeAnimationDuration);
            }

            yield return new WaitForSeconds(vulnTime);


            // -----------------------------------------------------
            // 2. Durum: KORUMAYA GEÇİYORUZ (Zırh geldi / Gömüldü)
            // -----------------------------------------------------
            IsVulnerable = false;
            float immuneTime = Random.Range(minImmuneDuration, maxImmuneDuration);
            Debug.Log($"[FirstBoss] Boss is IMMUNE for {immuneTime:F1} seconds!");

            // Objeyi tekrar kafanın içine doğru göm (Animasyonlu)
            if (weakPointObj != null)
            {
                // Boss'un vücuduna Local y ekseninde saklanacak
                weakPointObj.DOLocalMoveY(hiddenYPos, exposeAnimationDuration).SetEase(Ease.InBack);

                // Opsiyonel: Gömülürken Gri/Sönük renge dönmesini isterseniz
                // weakPointObj.GetComponent<MeshRenderer>().material.DOColor(Color.gray, exposeAnimationDuration);
            }

            yield return new WaitForSeconds(immuneTime);
        }
    }

    // ── IDLE ────────────────────────────────────────────────────
    private void StateIdle()
    {
        idleTimer -= Time.deltaTime;
        
        // Pick between Bounce or Dash and go to attack
        if (idleTimer <= 0f)
            EnterState(PickNextState()); 
    }

    // ── BOUNCE ──────────────────────────────────────────────────
    private void StateBounce()
    {
        // Enforce velocity strictly to our direction to prevent mid-air slow downs
        rb.linearVelocity = bounceDirection * bounceSpeed;

        if (!bounceHit)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, bounceRadius, playerMask);
            if (hits.Length > 0)
            {
                bounceHit = true;
                Collider playerCol = hits[0];

                IDamagable damageableTarget = playerCol.GetComponent<IDamagable>();
                if (damageableTarget != null)
                {
                    Vector3 pushDirection = gizmoDashDir;
                    damageableTarget.TakeDamage(attackDamage, timeStopScale, timeStopDuration);
                }

                Debug.Log($"[FirstBoss] Bounce → Player'a Sanal çarptı! {attackDamage} hasar");
            }
        }
    }

    private IEnumerator DashRoutine(Vector3 startPos, Vector3 targetPos)
    {
        float elapsed = 0f;
        Debug.Log($"[FirstBoss] Dash → Başladı! Hedef Z: {targetPos.z:F1}, Mesafe: {(targetPos - startPos).magnitude:F1}");
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;

            // Eğri üzerinden yumuşatılmış t değerini al (Yavaş başlayıp hızlansın diye)
            float curveT = dashCurve.Evaluate(t);

            // MovePosition fizikleri hesaba katarak Boss'u ışınlar/sürükler
            rb.MovePosition(Vector3.LerpUnclamped(startPos, targetPos, curveT));

            // Eğer dash sırasındayken bir fiziksel kuvvet olursa, es geç
            rb.linearVelocity = Vector3.zero;

            yield return null;
        }

        // Kesin hedefe oturt ve bitir
        rb.MovePosition(targetPos);

        Debug.Log("[FirstBoss] Dash → Hedefe vardı, süre doldu. Back to Idle.");
        EnterState(FirstBossState.Idle);
    }

    // Bounce yansıması — OnCollisionEnter
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != FirstBossState.Bounce) return;
        if (isBouncing) return;
        if ((wallMask.value & (1 << collision.gameObject.layer)) == 0) return;


        // --- BURADAN AŞAĞISI STANDART SEKME (WALL BOUNCE VEYA PLAYER BOUNCE) MANTIĞI ---

        bounceHit = false;

        Vector3 normal = collision.contacts[0].normal;
        normal.x = 0f; // Force entirely onto the 2.5D plane
        if (normal.sqrMagnitude < 0.001f) return;
        normal = normal.normalized;

        // Has it hit the ground? (Normal pointing straight up)
        if (normal.y > 0.5f)
        {
            currentGroundBounces++;
            if (currentGroundBounces >= maxGroundBounces)
            {
                // After bouncing off the ground requested amount of times, go back back to Idle
                EnterState(FirstBossState.Idle);
                return;
            }
        }

        // Bilardo yansıması: r = d - 2(d·n)n
        float dot = Vector3.Dot(bounceDirection, normal);
        bounceDirection = (bounceDirection - 2f * dot * normal).normalized;
        bounceDirection.x = 0f; // enforce 2.5D strictly
        bounceDirection = bounceDirection.normalized;

        gizmoBounceDir = bounceDirection;

        rb.linearVelocity = bounceDirection * bounceSpeed;
    }

    private IEnumerator BounceGuard()
    {
        isBouncing = true;
        yield return new WaitForSeconds(0.05f);
        isBouncing = false;
    }

    // ── DASH ────────────────────────────────────────────────────
    private void StateDash()
    {
        // Hareket tamamen DashRoutine (Coroutine) tarafından yapılıyor.
        // Burada SADECE Player'a çarpıp çarpmadığı check ediliyor:
        if (!dashHit)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, bounceRadius, playerMask);
            if (hits.Length > 0)
            {
                dashHit = true;
                Collider playerCol = hits[0];

                IDamagable damageableTarget = playerCol.GetComponent<IDamagable>();
                if (damageableTarget != null)
                {
                    Vector3 pushDirection = gizmoDashDir;
                    damageableTarget.TakeDamage(attackDamage, timeStopScale, timeStopDuration);
                }

                Debug.Log($"[FirstBoss] Dash → Player'a Sanal çarptı! {attackDamage} hasar");
            }
        }
    }

    // ── WEIGHTED RANDOM ─────────────────────────────────────────
    private FirstBossState PickNextState()
    {
        // Sadece Z ekseni farklılıklarını alarak tamamen "Yatay uzaklığı" buluyoruz. (2.5D için X kullanılmaz)
        float horizontalDistance = Mathf.Abs(playerTransform.position.z - transform.position.z);

        // Sadece Y ekseni farklılıklarını alarak "Dikey uzaklığı" (Yükseklik farkı) buluyoruz.
        float verticalDistance = playerTransform.position.y;

        // ŞARTINIZ: Player belli bir yükseklikten (örn: 2 birim) küçük VE yeterince uzak (örn: 10 birim) ise:
        if (verticalDistance < maxVertical && horizontalDistance > minHorizantal)
        {
            Debug.Log($"[FirstBoss] Player şartlara uyuyor (Yatay: {horizontalDistance:F1}, Dikey: {verticalDistance:F1}). Otomatik DASH!");
            return FirstBossState.Dash;
        }

        return FirstBossState.Bounce;

        //float total = weightBounce + weightDash;
        //float roll = Random.Range(0f, total);

        //// This ensures the boss natively selects an attack
        //if (roll < weightBounce) return FirstBossState.Bounce;
        //else return FirstBossState.Dash;
    }


    public void TriggerFirstBossDeath()
    {
        StartCoroutine(BossDeathAnimation());
    }

    
    private IEnumerator BossDeathAnimation()
    {
        // 1. Titreme
        Debug.Log("boss death animation started ");
        EnterState(FirstBossState.Death);
        float elapsed = 0f;
        Vector3 originalPos = transform.position;
        while (elapsed < 1f)
        {
            transform.position = originalPos + Random.insideUnitSphere * 0.1f;

            if (shakeVfxInterval > 0f)
            {
                vfxTimer += Time.deltaTime;
                if (vfxTimer >= shakeVfxInterval)
                {
                    PlayVFX();
                    vfxTimer = 0f;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        

        // 3. Küçülme + son VFX
        PlayVFX();
        transform.localScale = originalScale;
        Tween shrinkTween = transform.DOScale(Vector3.zero, shrinkDuration).SetEase(shrinkEase);
        PlayVFX();
        yield return shrinkTween.WaitForCompletion();

        PlayVFX();

        Destroy(gameObject , 0.2f);
        GameEvents.current.TriggerBossDeathAnimationEnd();
    }

    private void PlayVFX()
    {
        VisualEffect vfxInstance = Instantiate(
            bossDeathVFX,
            transform.position,
            Quaternion.identity
        );
        vfxInstance.Play();

        // Bittikten sonra otomatik sil
        Destroy(vfxInstance.gameObject, 3f);
    }

    public override void EnemyAttack() { }
    public override void EnemyMovement() { }

    // ── GIZMOS ──────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bounceRadius);

        switch (currentState)
        {
            case FirstBossState.Bounce:
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, gizmoBounceDir * 3f);
                Gizmos.DrawWireSphere(transform.position + gizmoBounceDir * 3f, 0.15f);
                break;

            case FirstBossState.Dash:
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, gizmoDashDir * 4f);
                Gizmos.DrawWireSphere(transform.position + gizmoDashDir * 4f, 0.15f);

                if (playerTransform != null)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    Gizmos.DrawLine(transform.position, playerTransform.position);
                }
                break;

            case FirstBossState.Idle:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, bounceRadius * 1.5f);
                break;
        }
    }
}
