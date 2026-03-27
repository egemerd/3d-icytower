using UnityEngine;

[CreateAssetMenu(fileName = "InvincibilitySkill", menuName = "Skills/Invincibility")]
public class InvincibilitySkill : SkillStrategy
{
    public float invincibilityDuration = 5f;

    public override void UseSkill(GameObject obj)
    {
        Debug.Log($"{obj.name} is now using invinsibility skill");
    }
}
