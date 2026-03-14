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
        player.Movement();
        if (!player.isMoving && player.IsGrounded())
        {
            player.ChangeState(new IdleState());
        }
    }
}
