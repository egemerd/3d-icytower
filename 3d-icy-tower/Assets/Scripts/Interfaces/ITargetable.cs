using UnityEngine;

public interface ITargetable 
{
    Transform GetTransform();
    void OnKilled();
    void OnLockOn(float lockOnDelay);
    void OnLockOff();


}
