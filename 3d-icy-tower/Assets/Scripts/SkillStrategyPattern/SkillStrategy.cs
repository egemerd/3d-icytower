using UnityEngine;

public abstract class SkillStrategy : ScriptableObject
{
    public abstract void UseSkill(GameObject obj);
}
