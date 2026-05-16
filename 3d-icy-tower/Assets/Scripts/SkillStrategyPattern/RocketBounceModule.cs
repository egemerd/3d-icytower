using UnityEngine;

public class RocketBounceModule : MonoBehaviour
{
    // The direction the ball is currently travelling, normalized.
    // This is the single source of truth. Speed is applied separately.
    private Vector2 travelDir; // Y = world Y,  X = world Z  (2D plane of the game)

    private float speed;
    private bool active = false;

    private Rigidbody rb;

    [SerializeField] private LayerMask wallMask;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!active) return;

        // Every physics frame: just keep moving in travelDir at speed.
        // No gravity. No zMomentum. No pipeline. Just this.
        rb.linearVelocity = DirToWorld(travelDir) * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!active) return;
        if ((wallMask.value & (1 << collision.gameObject.layer)) == 0) return;

        // Get the wall normal and flatten it to our 2D plane (Y/Z)
        Vector3 n3 = collision.contacts[0].normal;
        Vector2 normal = new Vector2(n3.y, n3.z); // Y stays Y, Z maps to our X axis

        // Ignore floor/ceiling only if you want — remove this if you
        // want full billiard bouncing off floors too.
        // if (Mathf.Abs(normal.x) > 0.7f) return; // this would ignore floors

        if (normal.sqrMagnitude < 0.001f) return; // degenerate contact, skip
        normal = normal.normalized;

        // Billiard reflection formula: r = d - 2(d·n)n
        // This is pure geometry — angle of incidence equals angle of reflection.
        float dot = Vector2.Dot(travelDir, normal);
        travelDir = (travelDir - 2f * dot * normal).normalized;

        // Immediately push the rigidbody out in the new direction.
        // This prevents the physics engine from seeing a second collision
        // on the next frame (which would cause stuttering).
        rb.linearVelocity = DirToWorld(travelDir) * speed;
        GetComponent<PlayerController>().SetZMomentum(rb.linearVelocity.z);
    }

    // Call this from RocketUltiHandler when the rocket fires
    public void Activate(Vector3 worldLaunchDirection, float launchSpeed, LayerMask walls)
    {
        wallMask = walls;
        speed = launchSpeed;
        active = true;

        // Convert the 3D launch direction to our 2D travel direction
        // World Y → travelDir.x (vertical)
        // World Z → travelDir.y (horizontal in game)
        travelDir = new Vector2(worldLaunchDirection.y, worldLaunchDirection.z).normalized;

        rb.useGravity = false;
        rb.linearVelocity = worldLaunchDirection.normalized * speed;
        GetComponent<PlayerController>().SetZMomentum(rb.linearVelocity.z);
    }

    // Call this from RocketUltiHandler to update speed during acceleration phase
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    // Call this from RocketUltiHandler when the rocket ends
    public void Deactivate()
    {
        active = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        GetComponent<PlayerController>().SetZMomentum(0f);
    }

    // Converts our internal 2D direction back to a 3D world velocity
    private Vector3 DirToWorld(Vector2 dir)
    {
        // travelDir.x = world Y component
        // travelDir.y = world Z component
        // world X is always 0 (2.5D game)
        return new Vector3(0f, dir.x, dir.y);
    }
}

