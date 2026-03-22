using UnityEngine;

public class MantleState : IState
{
    private float timeInMantle = 0f;
    public void EnterState(PlayerController player)
    {
        Debug.Log("Entering Mantle State");
        timeInMantle = 0f;
        player.CallMantleJumpCoroutine();
    }

    public void ExitState(PlayerController player)
    {
        player.StopAllCoroutines();
        player.UnlockFromMantle();
        Debug.Log("Exiting Mantle State");
    }

    public void UpdateState(PlayerController player)
    {
        timeInMantle += Time.deltaTime;

        // Check if the player pressed Jump during the mantle
        if (player.CheckJumpInput()) // This method uses InputManager.Instance.ConsumeJumpPressed()
        {
            player.MantleNormalJump();

            // If pressed within the exact timeline window
            if (timeInMantle <= player.MantleBoostTimer)
            {
                player.MantleBoostJump();
            }
            else // Late press
            {
                player.MantleNormalJump();
            }

            // Immediately switch to jumping state, interrupting the mantle
            player.ChangeState<JumpingState>();
        }
    }
}
