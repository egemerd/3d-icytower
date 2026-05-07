using UnityEngine;

public class WalkingState : IState
{
    private ParticleSystem walkDust;

    public void EnterState(PlayerController player)
    {
        player.animator.SetBool("isWalking", true);
        walkDust = ParticleEffects.Instance.PlayLooping(ParticleType.WalkDust, player.transform, new Vector3(0, -1f, 0));

        Debug.Log("Entered Walking State");
    }

    public void ExitState(PlayerController player)
    {
        player.animator.SetBool("isWalking", false);
        ParticleEffects.Instance.StopLooping(walkDust, ParticleType.WalkDust);

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
