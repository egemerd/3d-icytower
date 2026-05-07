using UnityEngine;

public class WalkingState : IState
{
    

    public void EnterState(PlayerController player)
    {
        player.animator.SetBool("isWalking", true);
        Debug.Log("Entered Walking State");
    }

    public void ExitState(PlayerController player)
    {
        player.animator.SetBool("isWalking", false);
        Debug.Log("Exited Walking State");
    }

    public void UpdateState(PlayerController player)
    {
        player.Movement();
        
        if (!player.isMoving && player.IsGrounded())
        {
            player.ChangeState<IdleState>();
        }
        else if (player.IsGrounded() && InputManager.Instance.jumpAction.IsPressed())
        {
            player.Jump();
            player.ChangeState<JumpingState>();
        }
    }
}
