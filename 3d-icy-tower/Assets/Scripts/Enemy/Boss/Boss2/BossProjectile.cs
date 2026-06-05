using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public int damage = 1;
    public LayerMask wallMask;
    public LayerMask playerMask;
    public float lifetime = 6f;

    private Rigidbody rb;
    private Vector3 moveDir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Launch(Vector3 direction)
    {
        moveDir = direction.normalized;
        rb.linearVelocity = moveDir * speed;
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        // Hýzý sabit tut (sürtünme vs etkilemesin)
        rb.linearVelocity = moveDir * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Duvara çarptý mý?
        if ((wallMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            Vector3 normal = collision.contacts[0].normal;
            normal.x = 0f; // 2.5D
            if (normal.sqrMagnitude < 0.001f) return;
            normal = normal.normalized;

            float dot = Vector3.Dot(moveDir, normal);
            moveDir = (moveDir - 2f * dot * normal).normalized;
            moveDir.x = 0f;
            moveDir = moveDir.normalized;

            rb.linearVelocity = moveDir * speed;
            return;
        }

        // Oyuncuya çarptý mý?
        if ((playerMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            IDamagable damagable = collision.gameObject.GetComponentInParent<IDamagable>();
            damagable?.TakeDamage(damage, 0f, 0.1f);
            Destroy(gameObject);
        }
    }
}