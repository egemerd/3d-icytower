using UnityEngine;

public abstract class Enemy : MonoBehaviour, ITargetable
{
    [SerializeField] private EnemyDataSO enemyData;
    [SerializeField] private Transform[] reticleCorners;

    private bool isLockedOn = false;

    private float currentLockTimer = 0f;
    private float targetLockDelay = 1f;

    private void Update()
    {
        if (isLockedOn)
        {
            UpdateReticleAnimation();
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnKilled()
    {
        Debug.Log("OnKilled");
    }

    private void UpdateReticleAnimation()
    {
        currentLockTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(currentLockTimer / targetLockDelay);

        if (reticleCorners != null && reticleCorners.Length == 4)
        {
            float currentDist = Mathf.Lerp(enemyData.reticleStartDistance, enemyData.reticleEndDistance, progress);

            // Yerel (Local) pozisyonlarý deðiþtirerek objeleri merkeze çeker
            reticleCorners[0].localPosition = new Vector3(-currentDist, currentDist, 0); // Sol üst
            reticleCorners[1].localPosition = new Vector3(currentDist, currentDist, 0);  // Sað üst
            reticleCorners[2].localPosition = new Vector3(currentDist, -currentDist, 0); // Sað alt
            reticleCorners[3].localPosition = new Vector3(-currentDist, -currentDist, 0);// Sol alt
        }
    }

    public void OnLockOff()
    {
        Debug.Log("Enemy lock lost!");
        isLockedOn = false;
        currentLockTimer = 0f;

        // Görselleri tekrar sakla
        foreach (var corner in reticleCorners)
        {
            if (corner != null) corner.gameObject.SetActive(false);
        }
    }

    public void OnLockOn(float lockOnDelay)
    {
        Debug.Log("Enemy locked on!");
        isLockedOn = true;
        currentLockTimer = 0f;
        targetLockDelay = lockOnDelay;

        // Görselleri aktif et
        foreach (var corner in reticleCorners)
        {
            if (corner != null) corner.gameObject.SetActive(true);
        }
    }
}
