using UnityEngine;

public class AttackingState : IState
{
    public void EnterState(PlayerController player)
    {
        player.isAttacking = true;
        player.Rb.linearVelocity = Vector3.zero;
        player.Rb.useGravity = false;
        player.PlayerAttackUndamagableEnter();
    }

    public void UpdateState(PlayerController player)
    {
        
    }

    public void ExitState(PlayerController player)
    {
        // State'ten çýkarken yerçekimini geri aç
        player.Rb.useGravity = true;
        player.isAttacking = false;
        player.PlayerAttackUndamagableExit();
    }
}
