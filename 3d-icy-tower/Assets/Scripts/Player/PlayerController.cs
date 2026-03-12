using UnityEngine;

public class PlayerController : MonoBehaviour
{
    InputManager input;
    public float speed = 8f;   
    Rigidbody rb;
    public bool isMoving;
    private IState currentState;

    [Header("Jump / Ground Check")]
    [SerializeField] private float jumpForce = 7f;          // upward impulse applied on jump
    [SerializeField] private Transform groundCheck;        // assign an empty child at player's feet
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundMask;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        input = InputManager.Instance;
        currentState = new IdleState();
        currentState.EnterState(this);
    }

    private void FixedUpdate()
    {
        currentState.UpdateState(this);
        if(input.jumpAction.triggered)
            Jump();
    }

    public void ChangeState(IState newState)
    {
        currentState.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }


    public void Movement()
    {
        float inputX = input.moveInput.x;

        // Preserve vertical velocity, set horizontal directly for tight arcade feel.
        Vector3 vel = rb.linearVelocity;
        vel.z = inputX * speed * Time.deltaTime;
        rb.linearVelocity = vel;
    }

    public void Jump()
    {
        if (!IsGrounded())
            return;

        // Clear any downward velocity and apply an immediate upward velocity change for predictable jump.
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    private bool IsGrounded()
    {
        // If a groundCheck Transform is provided, use sphere check; else fallback to a short raycast.
        if (groundCheck != null)
        {
            return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        }

        // fallback - raycast from object position downward
        float checkDistance = 0.2f;
        return Physics.Raycast(transform.position, Vector3.down, checkDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
