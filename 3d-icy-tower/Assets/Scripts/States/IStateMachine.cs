using UnityEngine;

public interface IStateMachine
{
    void ChangeState<T>() where T : IState;
}
