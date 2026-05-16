using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SkillSlot
{
    // Bu sadece Inspector'da "Skill 1 (Q Tuþu)" vb. þeklinde not almak içindir.
    // Kod mantýðýna etki etmez, ama vizyona yardýmcý olur.
    public string slotName;
    public SkillStrategy skill;
}

public class PlayerSkills : MonoBehaviour
{
    [Header("Equipped Skills")]
    [SerializeField] private List<SkillSlot> equippedSkills = new List<SkillSlot>();

    private void Update()
    {
        HandleSkillInput();
    }

    void HandleSkillInput()
    {
        // GetSkillIndex() now consumes the press, so this only fires ONCE
        // per button press no matter how many frames Update() runs.
        int skillIndex = InputManager.Instance.GetSkillIndex();

        if (skillIndex >= 0 && skillIndex < equippedSkills.Count)
        {
            var currentSkill = equippedSkills[skillIndex].skill;

            if (currentSkill != null)
            {
                currentSkill.UseSkill(this.gameObject);
                Debug.Log($"Skill used: {equippedSkills[skillIndex].slotName}");
            }
            else
            {
                Debug.LogWarning($"Skill slot (Index: {skillIndex}) is empty!");
            }
        }
    }

}