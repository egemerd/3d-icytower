using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputManager input;
    private Vector2 moveInput;
    private Rigidbody rb;
    private IState currentState;

    [Header("Movement")]
    [SerializeField] private float startSpeed = 3f;
    [SerializeField] private float targetReachSpeed = 10f;
    [SerializeField] private float accelerationTime = 0.3f;
    [SerializeField] private float decelerationTime = 0.2f;
    [SerializeField] private float decelerationAmount = 1.5f;
    [SerializeField] private float accelerationAmount = 1.1f;


    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundMask;

    public bool isMoving { get; private set; }

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

    private void Update()
    {
        moveInput = input.moveInput;
        isMoving = Mathf.Abs(moveInput.x) > 0.1f;
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
        Vector3 velocity = rb.linearVelocity;
        float currentZ = velocity.z;
        float inputX = moveInput.x;

        if (Mathf.Abs(inputX) > 0.1f)
        {
            float direction = Mathf.Sign(inputX);
            float initialSpeed = Mathf.Min(startSpeed, targetReachSpeed);

            if (Mathf.Abs(currentZ) < 0.01f || Mathf.Sign(currentZ) != direction)
            {
                currentZ = direction * initialSpeed;
            }

            float baseAccelerationRate = Mathf.Abs(targetReachSpeed - initialSpeed) / Mathf.Max(0.001f, accelerationTime);
            float accelerationRate = baseAccelerationRate * Mathf.Max(0f, accelerationAmount);
            float targetZ = direction * targetReachSpeed;

            currentZ = Mathf.MoveTowards(currentZ, targetZ, accelerationRate * Time.fixedDeltaTime);
        }
        else
        {
            float baseDecelerationRate = targetReachSpeed / Mathf.Max(0.001f, decelerationTime);
            float decelerationRate = baseDecelerationRate * Mathf.Max(0f, decelerationAmount);

            currentZ = Mathf.MoveTowards(currentZ, 0f, decelerationRate * Time.fixedDeltaTime);
        }

        velocity.z = currentZ;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    

    public void Jump()
    {
        Vector3 velocity = rb.linearVelocity;

        // Calculate rate of stop: How fast to go from currentSpeed to 0 within 'decelerationTime'
        float decelerationRate = targetReachSpeed / decelerationTime;

        // Bring velocity down to 0 smoothly 
        velocity.z = Mathf.MoveTowards(velocity.z, 0f, decelerationRate * Time.fixedDeltaTime);
        velocity.x = 0f;

        rb.linearVelocity = velocity;
    }

    

    

   

    public void InAirMovement()
    {
        
    }   

    private bool IsGrounded()
    {
        if (groundCheck != null)
        {
            return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        }

        float checkDistance = 0.25f;
        return Physics.Raycast(transform.position, Vector3.down, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
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