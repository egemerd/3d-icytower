using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PogoStickSkill", menuName = "Skills/PogoStick")]
public class PogoStickSkill : SkillStrategy
{
    [Header("Pogo Physics")]
    public float pogoForce = 15f;
    public float pogoDetectionHeight = 5f;
    public float pogoDetectionDuration = 0.5f;
    public LayerMask groundLayer;

    [Header("Pogo Model")]
    [Tooltip("Tüm pogo stick modeli (parent prefab)")]
    public GameObject pogoModelPrefab;

    [Tooltip("Prefab içinde scale'i uzayacak child'ın adı")]
    public string rodChildName = "PogoRod";

    [Tooltip("Spawn pozisyonu için oyuncu ayak offset'i")]
    public Vector3 spawnOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Rod Scale")]
    [Tooltip("Çubuğun başlangıç Z scale'i")]
    public float rodStartScaleZ = 0.1f;

    [Tooltip("Çubuğun maksimum uzayacağı Z scale değeri")]
    public float rodMaxScaleZ = 3f;

    [Header("Retract Settings")]
    [Tooltip("Çarpma sonrası çubuğun geri çekilme süresi")]
    public float retractDuration = 0.3f;

    public override void UseSkill(GameObject obj)
    {
        PlayerController controller = obj.GetComponent<PlayerController>();
        if (controller == null) return;

        // MonoBehaviour olmadığımız için controller üzerinden coroutine başlatıyoruz
        controller.StartCoroutine(PogoRoutine(obj, controller));
    }

    private IEnumerator PogoRoutine(GameObject obj, PlayerController controller)
    {
        // ── 1. Modeli oyuncunun ayağında spawn et ────────────────────────
        GameObject modelInstance = Instantiate(
            pogoModelPrefab,
            obj.transform.position + spawnOffset,
            obj.transform.rotation,
            obj.transform  // oyuncuya attach
        );
        modelInstance.transform.localPosition = spawnOffset;

        // ── 2. Rod child'ını bul ─────────────────────────────────────────
        Transform rod = modelInstance.transform.Find(rodChildName);
        if (rod == null)
        {
            Debug.LogWarning($"PogoStickSkill: '{rodChildName}' adlı child bulunamadı!");
            Destroy(modelInstance);
            yield break;
        }

        // Rod'un başlangıç local scale'ini kaydet, sadece Z'yi değiştireceğiz
        Vector3 originalScale = rod.localScale;
        rod.localScale = new Vector3(originalScale.x, originalScale.y, rodStartScaleZ);

        // ── 3. Extend: çubuğu Z yönünde uzat, raycast ile yer kontrol et ─
        float elapsed = 0f;
        bool hitGround = false;

        while (elapsed < pogoDetectionDuration)
        {
            if (obj == null) { Destroy(modelInstance); yield break; }

            elapsed += Time.deltaTime;
            float t = elapsed / pogoDetectionDuration;

            float currentScaleZ = Mathf.Lerp(rodStartScaleZ, rodMaxScaleZ, t);
            rod.localScale = new Vector3(originalScale.x, originalScale.y, currentScaleZ);

            // Z scale'den gerçek dünya uzunluğunu hesapla
            float worldLength = currentScaleZ * originalScale.z;

            // Oyuncudan aşağı raycast
            if (Physics.Raycast(obj.transform.position, Vector3.down, out RaycastHit hit, worldLength, groundLayer))
            {
                Debug.Log($"Pogo hit: {hit.collider.name}");
                hitGround = true;
                break;
            }

            yield return null;
        }

        // ── 4. Zıplama ───────────────────────────────────────────────────
        if (hitGround)
        {
            PerformPogoJump(controller);
        }

        // ── 5. Retract: çubuğu geri çek ─────────────────────────────────
        float currentZ = rod.localScale.z;
        float retractElapsed = 0f;

        while (retractElapsed < retractDuration)
        {
            if (modelInstance == null) yield break;

            retractElapsed += Time.deltaTime;
            float t = retractElapsed / retractDuration;

            float scaleZ = Mathf.Lerp(currentZ, rodStartScaleZ, t);
            rod.localScale = new Vector3(originalScale.x, originalScale.y, scaleZ);

            yield return null;
        }

        // ── 6. Temizle ───────────────────────────────────────────────────
        if (modelInstance != null)
            Destroy(modelInstance);
    }

    private void PerformPogoJump(PlayerController controller)
    {
        Vector3 vel = controller.Rb.linearVelocity;
        vel.y = 0f;
        controller.Rb.linearVelocity = vel;

        controller.Rb.AddForce(Vector3.up * pogoForce, ForceMode.VelocityChange);
        controller.ChangeState<JumpingState>();
    }
}