using UnityEngine;

[CreateAssetMenu(fileName = "PogoStickSkill", menuName = "Skills/PogoStick")]
public class PogoStickSkill : SkillStrategy
{
    public override void UseSkill(GameObject obj)
    {
        // Objeye zýplama yeteneði kazandýr
        // Örneðin, objeye bir "PogoStick" bileþeni ekleyebilir veya objenin hareket mekanizmasýný geçici olarak deðiþtirebilirsiniz.
        Debug.Log($"{obj.name} is now using a pogo stick!");
    }
}  
