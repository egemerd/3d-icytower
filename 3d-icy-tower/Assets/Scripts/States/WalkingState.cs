using UnityEngine;

public class WalkingState : IState
{
    public void EnterState(PlayerController player)
    {
        Debug.Log("Entered Walking State");
    }

    public void ExitState(PlayerController player)
    {
        Debug.Log("Exited Walking State");
    }

    public void UpdateState(PlayerController player)
    {
        player.Movement();
        if (!player.isMoving && player.IsGrounded())
        {
            player.ChangeState(new IdleState());
        }
        if (player.IsGrounded() && InputManager.Instance.isJumpingTriggered)
        {
            player.Jump();
            player.ChangeState(new JumpingState());
        }
    }
}
