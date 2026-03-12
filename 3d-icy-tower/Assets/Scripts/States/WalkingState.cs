using UnityEngine;

public class WalkingState : IState
{
    public void EnterState(PlayerController player)
    {
        Debug.Log("Entered Walking State");
    }

    public void ExitState(PlayerController player)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateState(PlayerController player)
    {
        player.Movement();
         if (!player.isMoving)
         {
             player.ChangeState(new IdleState());
         }
    }
}
