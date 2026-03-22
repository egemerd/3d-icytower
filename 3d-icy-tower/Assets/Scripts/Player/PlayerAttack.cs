using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private IStateMachine stateMachine;

    private void Awake()
    {
        stateMachine = GetComponent<IStateMachine>();
    }

    public void Attack()
    {

    }

    private IEnumerator AttackCoroutine()
    {
        yield return null;
    }
}
