using UnityEngine;

public class PlayerMantleHandler : MonoBehaviour
{
    [Header("Mantle Raycast Settings")]
    [SerializeField] private float raycastDistance = 2f;
    [SerializeField] private LayerMask mantleMask;
    [SerializeField] private Vector3 originOffset = new Vector3(0, 1f, 0);

    [Header("Velocity Settings")]
    [SerializeField] private float minMomentumValueToCancelCollider = 10f;

    private Collider playerCollider;
    private PlayerController playerController;
    private Collider currentlyIgnoredPlatform; // We cache this so we don't call it repeatedly

    private void Start()
    {
        playerController = GetComponent<PlayerController>(); 
        playerCollider = GetComponent<Collider>(); 
    }

    private void Update()
    {
        MantleCheckRaycast();
    }

    void MantleCheckRaycast()
    {
        // 1. Check requirements: Are we moving fast horizontally AND moving upwards?
        bool isMovingFast = Mathf.Abs(playerController.zMomentum) > minMomentumValueToCancelCollider;
        bool isMovingUp = playerController.Rb.linearVelocity.y > 0.1f;

        if (isMovingFast && isMovingUp)
        {
            Vector3 origin = transform.position + originOffset;

            // Notice we only perform the Raycast if the player is actually moving fast and up. (huge performance save)
            if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, raycastDistance, mantleMask))
            {
                if (hit.collider.CompareTag("ClimbPoint"))
                {
                    // Check if we haven't already ignored this specific platform
                    if (currentlyIgnoredPlatform != hit.collider)
                    {
                        currentlyIgnoredPlatform = hit.collider;

                        // OPTIMAL: Tell Unity to just let the Player pass through this specific item seamlessly
                        Physics.IgnoreCollision(playerCollider, currentlyIgnoredPlatform, true);
                    }
                }
            }
        }
        else if (currentlyIgnoredPlatform != null && playerController.Rb.linearVelocity.y <= 0f)
        {
            // 2. We are no longer moving up (we are falling down), or we slowed down! 
            // We MUST restore the collision immediately so the player can land normally on top.
            Physics.IgnoreCollision(playerCollider, currentlyIgnoredPlatform, false);
            currentlyIgnoredPlatform = null;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + originOffset;
        Vector3 direction = Vector3.up;

        // Draw the main line of the raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + (direction * raycastDistance));

        // Let's do a live check in the editor to color the tip of the raycast
        if (Physics.Raycast(origin, direction, out RaycastHit hit, raycastDistance, mantleMask))
        {
            // Hit successful - draw a green sphere at the hit point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
        else
        {
            // No hit - draw a red sphere at the max distance
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(origin + (direction * raycastDistance), 0.1f);
        }
    }
}
