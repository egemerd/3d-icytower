using UnityEngine;

public class JumpingState : IState
{
    public void EnterState(PlayerController player)
    {   
        player.animator.SetBool("isJumping",true);
        Debug.Log("Entered Jumping State");
    }

    public void ExitState(PlayerController player)
    {
        player.animator.ResetTrigger("Jump");
        player.animator.SetBool("isJumping", false);
        Debug.Log("Exited Jumping State");
    }

    public void UpdateState(PlayerController player)
    {
        player.InAirMovement();
        //player.HandleGravity();

        if (player.Rb.linearVelocity.y <= 0.1f && player.IsGrounded())
        {
            if (player.isMoving)
            {
                player.ChangeState<WalkingState>();
            }
            else
            {
                player.ChangeState<IdleState>();
            }
        }
    }
}
