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
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Debug")]
    [SerializeField] private bool showCurrentStateOnScreen = true;

    private GUIStyle stateLabelStyle;
    public Rigidbody Rb => rb;

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
    public bool CheckJumpInput()
    {
        return InputManager.Instance.ConsumeJumpPressed();
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
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        

    }







    public void InAirMovement()
    {
        Movement();
    }   

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        Vector3 end = origin + direction * groundCheckDistance;

        bool grounded = Physics.Raycast(
            origin,
            direction,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawRay(origin, direction * groundCheckDistance);
        Gizmos.DrawWireSphere(end, 0.03f);
    }

    private void OnGUI()
    {
        if (!showCurrentStateOnScreen || currentState == null)
        {
            return;
        }

        if (stateLabelStyle == null)
        {
            stateLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        string stateName = currentState.GetType().Name;
        GUI.Label(new Rect(10f, 10f, 500f, 24f), "State: " + stateName, stateLabelStyle);
    }
}