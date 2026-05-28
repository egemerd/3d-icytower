using UnityEngine;

public class IdleState : IState
{
    public void EnterState(PlayerController player)
    {
        player.ResetRotationToForward();
        player.animator.SetBool("isIdle", true);    
    }

    public void ExitState(PlayerController player)
    {
        player.animator.SetBool("isIdle", false);
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
