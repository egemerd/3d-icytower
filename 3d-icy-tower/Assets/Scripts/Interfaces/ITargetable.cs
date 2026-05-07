using UnityEngine;

public interface ITargetable 
{
    Transform GetTransform();
    void OnKilled();
    void OnLockOn(float lockOnDelay);
    void OnLockOff();

    bool IsInTimingWindow { get; }
    void StartTimingUI();
    void StopTimingUI();


}
