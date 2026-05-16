using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Ulti Skill", menuName = "Skills/Ulti Skill")]
public class UltiSkill : SkillStrategy
{
    [Header("Arrow / Aiming")]
    [Tooltip("Prefab that shows direction while aiming. Its local 'up' should point forward.")]
    public GameObject arrowPrefab;

    [Tooltip("Degrees per second the arrow oscillates back and forth.")]
    public float aimOscillationSpeed = 100f;

    [Header("Rocket – Phase 1: Impact (constant speed)")]
    [Tooltip("The launch speed applied instantly on fire. Feels like a cannon shot.")]
    public float rocketSpeed = 35f;

    [Tooltip("How many seconds the rocket holds this exact speed before accelerating.")]
    public float rocketDuration = 0.4f;

    [Header("Rocket – Phase 2: Surge (speed increases)")]
    [Tooltip("The maximum speed reached at the end of the acceleration ramp.")]
    public float rocketMaxSpeed = 80f;

    [Tooltip("How many seconds it takes to ramp from rocketSpeed up to rocketMaxSpeed.")]
    public float rocketAccelerationDuration = 1.2f;

    [Header("Rocket – Phase 3: Peak hold then stop")]
    [Tooltip("How long the rocket holds at peak speed before control is returned to the player.")]
    public float rocketPeakHoldDuration = 0.2f;

   


    public override void UseSkill(GameObject obj)
    {
        RocketUltiHandler handler = obj.AddComponent<RocketUltiHandler>();
        Debug.Log("Ulti Skill used: " );
        handler.StartUlti(this);
    }

}
