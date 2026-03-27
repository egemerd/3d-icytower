using UnityEngine;
using System.Threading.Tasks;


[CreateAssetMenu(fileName = "PogoStickSkill", menuName = "Skills/PogoStick")]
public class PogoStickSkill : SkillStrategy
{
    [Header("Pogo Physics")]
    public float pogoForce = 15f;
    public float pogoDetectionHeight = 5f;
    public float pogoDetectionDuration = 0.5f;
    public LayerMask groundLayer;

    [Header("Visuals")]
    [Tooltip("Aþaðý doðru uzayacak görsel (Örn: Çubuk sprite'ý içeren bir Prefab)")]
    public GameObject pogoVisualPrefab;

    public override void UseSkill(GameObject obj)
    {
        Debug.Log($"{obj.name} is now using a pogo stick!");
        // Metodu çaðýr ve ana thread'i bloklamadan çalýþmasýný saðla
        ExecutePogoAsync(obj);
    }

    private async void ExecutePogoAsync(GameObject obj)
    {
        if (obj == null) return;

        PlayerController controller = obj.GetComponent<PlayerController>();
        if (controller == null) return;

        float elapsed = 0f;
        GameObject visualInstance = null;
        Transform visualTransform = null;

        // Görseli oluþtur
        if (pogoVisualPrefab != null)
        {
            // Quaternion.identity yerine Quaternion.Euler(0, 90, 0) vererek baþtan 90 derece döndürülmüþ spawn ediyoruz.
            visualInstance = Instantiate(pogoVisualPrefab, obj.transform.position, Quaternion.Euler(0, 90, 0), obj.transform);
            visualTransform = visualInstance.transform;

            // Opsiyonel: Eðer rotasyonun Player'a göre her zaman lokal olarak (0, 90, 0) kalmasýný istiyorsan bu satýrý da kullanabilirsin:
            visualTransform.localRotation = Quaternion.Euler(0, 90, 0);

            visualTransform.localScale = new Vector3(0.3f, 0f, 1f);


        }

        bool hasHitTarget = false;

        // Belirlenen süre boyunca döngüyü iþlet
        while (elapsed < pogoDetectionDuration)
        {
            // Eðer obje yok edildiyse iþlemi güvenli bir þekilde iptal et
            if (obj == null) break;

            elapsed += Time.deltaTime;
            float t = elapsed / pogoDetectionDuration;
            float currentLength = Mathf.Lerp(0f, pogoDetectionHeight, t);

            // Görseli aþaðýya doðru uzat
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(0.3f, currentLength, 1f);
                visualTransform.localPosition = new Vector3(0f, -currentLength / 2f, 0f); // Uzadýkça aþaðý kaydýr
            }

            // Raycast ile yeri kontrol et
            if (Physics.Raycast(obj.transform.position, Vector3.down, out RaycastHit hit, currentLength, groundLayer))
            {
                Debug.Log("Pogo stick hit the ground!");
                Destroy(visualInstance);
                PerformPogoJump(controller);
                hasHitTarget = true;
                break; // Hedefe ulaþtý, döngüyü bitir
            }

            // Unity'nin bir sonraki frame'e geçmesini bekle (yield return null ile ayný iþi yapar)
            await Task.Yield();
        }

        // Görselin anýnda yok olmamasý için ufak bir bekleme süresi (opsiyonel)
        if (hasHitTarget)
        {
            await Task.Delay(200); // 0.2 saniye bekle
        }

        // Ýþlem bittikten sonra görseli temizle
        if (visualInstance != null)
        {
            Destroy(visualInstance);
        }
    }

    private void PerformPogoJump(PlayerController controller)
    {
        // Oyuncunun dikey hýzýný sýfýrla ki mevcut düþme hýzý zýplamayý yavaþlatmasýn
        Vector3 vel = controller.Rb.linearVelocity;
        vel.y = 0f;
        controller.Rb.linearVelocity = vel;

        // Pogo kuvvetini uygula
        controller.Rb.AddForce(Vector3.up * pogoForce, ForceMode.VelocityChange);

        // Zýplama state'ine geçiþ yap
        controller.ChangeState<JumpingState>();
    }
}  
