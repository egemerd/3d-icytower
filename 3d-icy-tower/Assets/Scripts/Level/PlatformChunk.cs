using UnityEngine;

public class PlatformChunk : MonoBehaviour
{
    [Tooltip("Place an Empty GameObject at the highest point of this chunk where the next chunk should start.")]
    public Transform connectionPoint;
    public Transform entryPoint;

    // Optional: You can add lists of enemies or targetables to reset them when the chunk is pooled
    public virtual void ResetChunk()
    {
        // Reset logic for when the chunk is pulled from the pool again
        // e.g., reactivate destroyed enemies, reset moving platforms
    }
    void OnDrawGizmos()
    {
        if (entryPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(entryPoint.position, 0.3f);
        }
        if (connectionPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(connectionPoint.position, 0.3f);
        }
    }
}
