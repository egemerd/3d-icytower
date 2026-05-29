using System.Collections;
using UnityEngine;

public class FirstBoss : Boss
{
    private enum FirstBossState { Idle, Bounce, Dash }
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
    [SerializeField] private int bounceDamage = 10;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float bounceRadius = 0.5f;

    private int currentGroundBounces = 0;
    private Vector3 bounceDirection;
    private bool isBouncing = false;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashDamage = 20f;
    private float dashTimer;
    private Vector3 dashDirection;
    private bool dashHit;

    private Rigidbody rb;

    // Gizmos
    private Vector3 gizmoBounceDir;
    private Vector3 gizmoDashDir;

    protected override void OnBossStart()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        EnterState(FirstBossState.Idle);
    }

    protected override void RunStateMachine()
    {
        switch (currentState)
        {
            case FirstBossState.Idle: StateIdle(); break;
            case FirstBossState.Bounce: StateBounce(); break;
            case FirstBossState.Dash: StateDash(); break;
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
                dashTimer = dashDuration;
                dashHit = false;
                dashDirection = (playerTransform.position - transform.position).normalized;
                dashDirection.x = 0f;
                dashDirection = dashDirection.normalized;
                gizmoDashDir = dashDirection;
                rb.linearVelocity = dashDirection * dashSpeed;
                break;
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

        Collider[] hits = Physics.OverlapSphere(transform.position, bounceRadius, playerMask);
        if (hits.Length > 0)
        {
            Debug.Log($"[FirstBoss] Bounce → Player'a çarptı! {bounceDamage} hasar");
            // hits[0].GetComponent<PlayerHealth>()?.TakeDamage(bounceDamage);
            
            // If you want the bounce to stop early when it hits the player, uncomment this:
            // EnterState(FirstBossState.Idle);
        }
    }

    // Bounce yansıması — OnCollisionEnter
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != FirstBossState.Bounce) return;
        if (isBouncing) return;
        if ((wallMask.value & (1 << collision.gameObject.layer)) == 0) return;
        

        // --- BURADAN AŞAĞISI STANDART SEKME (WALL BOUNCE VEYA PLAYER BOUNCE) MANTIĞI ---

        

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
        dashTimer -= Time.deltaTime;
        rb.linearVelocity = dashDirection * dashSpeed;

        // When dash time is up, return to Idle instead of immediately attacking again
        if (dashTimer <= 0f)
        {
            Debug.Log("[FirstBoss] Dash → Süre doldu. Back to Idle.");
            EnterState(FirstBossState.Idle); 
            return;
        }

        if (!dashHit)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, bounceRadius, playerMask);
            if (hits.Length > 0)
            {
                dashHit = true;
                Debug.Log($"[FirstBoss] Dash → Player'a çarptı! {dashDamage} hasar");
                // hits[0].GetComponent<PlayerHealth>()?.TakeDamage(dashDamage);
                
                // Finished dash attack, return to Idle
                EnterState(FirstBossState.Idle); 
            }
        }
    }

    // ── WEIGHTED RANDOM ─────────────────────────────────────────
    private FirstBossState PickNextState()
    {
        float total = weightBounce + weightDash;
        float roll = Random.Range(0f, total);

        // This ensures the boss natively selects an attack
        if (roll < weightBounce) return FirstBossState.Bounce;
        else return FirstBossState.Dash;
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
