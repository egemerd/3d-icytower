using UnityEngine;

public class PlayerController : MonoBehaviour
{
    InputManager input;
    public int speed = 100;   
    Rigidbody rb;
    public bool isMoving;
    private IState currentState;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = InputManager.Instance;
    }
    private void Start()
    {
        currentState = new IdleState();
        currentState.EnterState(this);
    }

    private void FixedUpdate()
    {
        currentState.UpdateState(this); 
    }

    public void ChangeState(IState newState)
    {
        currentState.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }


    public void Movement()
    {
        rb.angularVelocity = speed * input.moveInput * Time.deltaTime;
    }
}
