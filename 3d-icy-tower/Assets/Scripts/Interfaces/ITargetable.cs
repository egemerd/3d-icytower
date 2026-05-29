using UnityEngine;

public interface ITargetable 
{
    Transform GetTransform();
    void OnKilled(int damage);
    void OnLockOn(float lockOnDelay);
    void OnLockOff();

    bool IsInTimingWindow { get; }
    bool IsInPerfectWindow { get; }

    bool CanBeAttackedFrom(Vector3 attackerPosition);


    void PerfectAttack();
    void StartTimingUI();
    void StopTimingUI();


}
