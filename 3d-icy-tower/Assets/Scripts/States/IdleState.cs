using UnityEngine;

public class IdleState : IState
{
    public void EnterState(PlayerController player)
    {
        Debug.Log("Entered Idle State");
    }

    public void ExitState(PlayerController player)
    {
        Debug.Log("Exited Idle State");
    }

    public void UpdateState(PlayerController player)
    {
        player.Movement();
        if (player.isMoving)
        {
            player.ChangeState<WalkingState>(); 
        }
        if (player.IsGrounded() && InputManager.Instance.jumpAction.IsPressed())
        {
            player.Jump();
            player.ChangeState<JumpingState>();
        }
        
    }
}
