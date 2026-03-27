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
        int skillIndex = InputManager.Instance.GetSkillIndex();

        if (skillIndex >= 0 && skillIndex < equippedSkills.Count)
        {
            // Ýlgili yapýdan (struct) sadece skill nesnesini alýyoruz
            var currentSkill = equippedSkills[skillIndex].skill;

            // Null kontrolü, eðer boþ bir slot varsa atla.
            if (currentSkill != null)
            {
                currentSkill.UseSkill(this.gameObject);
                Debug.Log($"oBJECT: {this.gameObject} (Slot: {equippedSkills[skillIndex].slotName})");
            }
            else
            {
                Debug.LogWarning($"Yetenek slotu (Index: {skillIndex}) boþ!");
            }
        }
    }
}