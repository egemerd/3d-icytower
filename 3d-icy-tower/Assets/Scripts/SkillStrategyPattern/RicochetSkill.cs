using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "RicochetSkill", menuName = "Skills/Ricochet")]
public class RicochetSkill : SkillStrategy
{

    [Header("Ricochet Settings")]
    [SerializeField] private float firstTargetRadius = 12f;
    [SerializeField] private float bounceRadius = 8f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private int maxEnemyHits = 3;

    private readonly Collider[] overlapResults = new Collider[32];

    public override void UseSkill(GameObject obj)
    {
        if (obj == null) return;

        Vector3 currentOrigin = obj.transform.position;
        var hitTargets = new HashSet<ITargetable>();

        for (int i = 0; i < maxEnemyHits; i++)
        {
            float radius = i == 0 ? firstTargetRadius : bounceRadius;
            ITargetable nextTarget = FindClosestTarget(currentOrigin, radius, hitTargets);

            if (nextTarget == null)
            {
                break;
            }

            // Skill impact
            nextTarget.OnKilled();

            hitTargets.Add(nextTarget);
            currentOrigin = nextTarget.GetTransform().position;
        }
    }

    private ITargetable FindClosestTarget(Vector3 origin, float radius, HashSet<ITargetable> excluded)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, radius, overlapResults, targetLayer);

        ITargetable closest = null;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapResults[i];
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
