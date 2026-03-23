using UnityEngine;

public class Enemy1 : Enemy, ITargetable
{
    public Transform GetTransform()
    {
        return transform;
    }

    public void OnKilled()
    {
        Debug.Log("OnKilled");
    }

    public void OnLockOff()
    {
        Debug.Log("OnLockOff");
    }

    public void OnLockOn()
    {
        Debug.Log("OnLockOn");
    }
}
