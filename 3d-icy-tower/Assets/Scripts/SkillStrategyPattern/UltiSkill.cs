using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Ulti Skill", menuName = "Skills/Ulti Skill")]
public class UltiSkill : SkillStrategy
{
    [Header("Ulti Settings")]
    public float rocketSpeed = 25f;
    public float rocketDuration = 5f;
    public GameObject arrowPrefab;

    public override void UseSkill(GameObject obj)
    {
        RocketUltiHandler handler = obj.AddComponent<RocketUltiHandler>();
        Debug.Log("Ulti Skill used: " );
        handler.StartUlti(this);
    }

}
