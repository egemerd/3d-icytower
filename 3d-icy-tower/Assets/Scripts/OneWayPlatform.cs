using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [Header("Platform Colliders")]
    [SerializeField] private Collider solidCollider;          // Üstte durulan gerçek collider
    [SerializeField] private Collider passThroughTrigger;     // Trigger zone (isTrigger = true)

    [Header("Tuning")]
    [SerializeField] private float reEnableOffset = 0.02f;    // Oyuncu tabaný platform üstüne ne kadar geçince collision geri gelsin
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<Collider> ignoredPlayers = new HashSet<Collider>();


    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        Rigidbody playerRb = other.attachedRigidbody;
        if (playerRb == null)
        {
            return;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!ignoredPlayers.Contains(other))
        {
            return;
        }

        Rigidbody playerRb = other.attachedRigidbody;
        if (playerRb == null)
        {
            return;
        }

        float playerBottom = other.bounds.min.y;
        float platformTop = solidCollider.bounds.max.y;

        // Oyuncu platformun üstüne çýktýysa ve artýk düþüyorsa collision geri aç
        if (playerBottom >= platformTop + reEnableOffset && playerRb.linearVelocity.y <= 0f)
        {
            Physics.IgnoreCollision(other, solidCollider, false);
            ignoredPlayers.Remove(other); 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!ignoredPlayers.Contains(other))
        {
            return;
        }

        Physics.IgnoreCollision(other, solidCollider, false);
        ignoredPlayers.Remove(other);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag);
    }
}