using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;          

[CreateAssetMenu(fileName = "RicochetSkill", menuName = "Skills/Ricochet")]
public class RicochetSkill : SkillStrategy
{
    [Header("Ricochet Settings")]
    [SerializeField] private GameObject projectilePrefab; // Atýlacak obje (örn: dönen bir disk)
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float firstTargetRadius = 12f;
    [SerializeField] private float bounceRadius = 8f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private int maxEnemyHits = 3;

    public override void UseSkill(GameObject obj)
    {
        if (obj == null) return;
       
        ExecuteRicochetAsync(obj);
    }

    private async void ExecuteRicochetAsync(GameObject playerObj)
    {
        Vector3 currentOrigin = playerObj.transform.position;
        var hitTargets = new HashSet<ITargetable>();
        
        // 1. Hedef seçilebilecek kimse yoksa yeteneði hiç baþlatma
        ITargetable firstTarget = FindClosestTarget(currentOrigin, firstTargetRadius, hitTargets);
        if (firstTarget == null)
        {
            Debug.Log("Sekecek hedef bulunamadý.");
            return; // Çýk
        }

        // 2. Ýlk hedefe doðru gidecek görsel objeyi yarat
        GameObject ricochetProjectile = null;
        if (projectilePrefab != null)
        {
            ricochetProjectile = Instantiate(projectilePrefab, currentOrigin, Quaternion.identity);
        }

        // 3. Sekme döngüsüne baþla
        for (int i = 0; i < maxEnemyHits; i++)
        {
            float radius = i == 0 ? firstTargetRadius : bounceRadius;
            ITargetable nextTarget = FindClosestTarget(currentOrigin, radius, hitTargets);

            if (nextTarget == null)
            {
                break; // Vuracak baþka kimse kalmadý
            }

            Transform targetTransform = nextTarget.GetTransform();

            // Projectile yavaþ yavaþ gitsin
            if (ricochetProjectile != null)
            {
                // Hedef yok olana veya hedefe varana kadar mermiyi yürüt
                while (targetTransform != null && ricochetProjectile != null)
                {
                    Vector3 targetPos = targetTransform.position;
                    Vector3 dir = (targetPos - ricochetProjectile.transform.position).normalized;
                    ricochetProjectile.transform.LookAt(targetPos);
                    float step = projectileSpeed * Time.deltaTime;
                    ricochetProjectile.transform.position = Vector3.MoveTowards(ricochetProjectile.transform.position, targetPos, step);

                    // Hedefe vardýysa döngüden çýk
                    if (Vector3.Distance(ricochetProjectile.transform.position, targetPos) < 0.2f)
                    {
                        break;
                    }

                    await Task.Yield(); // Bir sonraki kareyi (frame) bekle
                }
            }

            // Vardýktan (veya hedefsiz anýnda ise) hasar vur
            if (nextTarget != null)
            {
                nextTarget.OnKilled();
                hitTargets.Add(nextTarget); // Tekrar ayný hedefe sekmemesi için listeye ekle
                
                if (targetTransform != null)
                {
                    currentOrigin = targetTransform.position;
                }
            }
        }

        // Sekme iþlemi bittiðinde objeyi yok et
        if (ricochetProjectile != null)
        {
            Destroy(ricochetProjectile);
        }
    }

    private ITargetable FindClosestTarget(Vector3 origin, float radius, HashSet<ITargetable> excluded)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, radius, new Collider[32], targetLayer);

        ITargetable closest = null;
        float closestSqr = float.MaxValue;

        Collider[] results = new Collider[32];
        count = Physics.OverlapSphereNonAlloc(origin, radius, results, targetLayer);

        for (int i = 0; i < count; i++)
        {
            Collider col = results[i];
            if (col == null) continue;

            if (!col.TryGetComponent(out ITargetable candidate)) continue;
            if (excluded.Contains(candidate)) continue;

            Vector3 candidatePos = candidate.GetTransform().position;
            float sqrDist = (candidatePos - origin).sqrMagnitude;

            if (sqrDist < closestSqr)
            {
                closestSqr = sqrDist;
                closest = candidate;
            }
        }

        return closest;
    }
}
