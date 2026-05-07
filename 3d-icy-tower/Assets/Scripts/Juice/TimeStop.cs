using System.Collections;
using UnityEngine;

public class TimeStop : MonoBehaviour
{
    // Singleton instance so any script can access it easily
    public static TimeStop Instance { get; private set; }

    [Header("Default Hitstop Settings")]
    [Tooltip("How long the game pauses by default (in unscaled seconds)")]
    [SerializeField] private float defaultStopDuration = 0.1f;
    
    [Tooltip("What timescale to drop to (0 = full pause, 0.05 = very slow)")]
    [SerializeField] private float defaultTimeScale = 0.05f;

    [Header("Restoration Settings")]
    [Tooltip("If true, time speeds back up smoothly. If false, it snaps back to 1 instantly.")]
    [SerializeField] private bool restoreSmoothly = false;
    
    [Tooltip("How fast time restores if Restore Smoothly is true")]
    [SerializeField] private float smoothRestoreSpeed = 5f;

    private Coroutine timeStopCoroutine;
    private float originalFixedDeltaTime;

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
            // Record original fixed delta time to prevent physics stuttering
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Stops time using the default settings in the Inspector.
    /// </summary>
    public void StopTime()
    {
        TriggerHitStop(defaultStopDuration, defaultTimeScale);
    }

    /// <summary>
    /// Stops time with specific duration and scale (Great for heavy hits vs light hits)
    /// </summary>
    public void StopTime(float duration, float targetTimeScale = 0f)
    {
        TriggerHitStop(duration, targetTimeScale);
    }

    private void TriggerHitStop(float duration, float scale)
    {
        if (timeStopCoroutine != null)
        {
            StopCoroutine(timeStopCoroutine);
        }
        timeStopCoroutine = StartCoroutine(HitStopRoutine(duration, scale));
    }

    private IEnumerator HitStopRoutine(float duration, float targetScale)
    {
        // 1. Slow down time
        Time.timeScale = targetScale;
        
        // Adjust fixed physics step so collisions don't bug out while slowed
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

        // 2. Wait for the duration (Using Realtime so the wait itself isn't slowed down)
        yield return new WaitForSecondsRealtime(duration);

        // 3. Restore Time
        if (restoreSmoothly)
        {
            while (Time.timeScale < 1f)
            {
                Time.timeScale += Time.unscaledDeltaTime * smoothRestoreSpeed;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
                yield return null;
            }
        }
        
        ForceRestoreTime();
    }

    /// <summary>
    /// Instantly restores time to normal.
    /// </summary>
    public void ForceRestoreTime()
    {
        if (timeStopCoroutine != null)
        {
            StopCoroutine(timeStopCoroutine);
            timeStopCoroutine = null;
            
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }
}
