using UnityEngine;

public class MantleState : IState
{
    public void EnterState(PlayerController player)
    {
        Debug.Log("Entering Mantle State");
        player.CallMantleJumpCoroutine();
    }

    public void ExitState(PlayerController player)
    {
        Debug.Log("Exiting Mantle State");
    }

    public void UpdateState(PlayerController player)
    {
        Debug.Log("Updating Mantle State");
    }
}
