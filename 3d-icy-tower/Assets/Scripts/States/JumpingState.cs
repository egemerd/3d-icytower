using UnityEngine;

public class JumpingState : IState
{
    public void EnterState(PlayerController player)
    {   
        Debug.Log("Entered Jumping State");
    }

    public void ExitState(PlayerController player)
    {
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
                player.ChangeState(new WalkingState());
            }
            else
            {
                player.ChangeState(new IdleState());
            }
        }
    }
}
