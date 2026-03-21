using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputManager input;
    private Vector2 moveInput;
    private Rigidbody rb;
    private IState currentState;

    private Dictionary<System.Type, IState> stateCache = new Dictionary<System.Type, IState>();

    [Header("Movement")]
    [SerializeField] private float startSpeed = 3f;
    [SerializeField] private float targetReachSpeed = 10f;
    [SerializeField] private float accelerationTime = 0.3f;
    [SerializeField] private float decelerationTime = 0.2f;
    [SerializeField] private float decelerationAmount = 1.5f;
    [SerializeField] private float accelerationAmount = 1.1f;

    [Header("Air Movement")]
    [SerializeField] private float maxAirSpeed = 10f;
    [SerializeField] private float airAcceleration = 12f; // How fast you speed up in the air
    [SerializeField] private float airDeceleration = 4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Wall Bounce")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float minBounceSpeed = 5f;
    [SerializeField] private float bounceSpeedMultiplier = 1.0f;
    [SerializeField] private bool groundCheckerForBounce = true;    

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 2.5f;
    [SerializeField] private float fallMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = 40f;

    [Header("Debug")]
    [SerializeField] private bool showCurrentStateOnScreen = true;

    private GUIStyle stateLabelStyle;
    public Rigidbody Rb => rb;

    // ARCADE PHYSICS TRACKERS
    private Vector3 lastFrameVelocity;
    private float zMomentum;

    public bool isMoving { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        input = InputManager.Instance;
        InitializeStates();
    }

    private void Update()
    {
        moveInput = input.moveInput;
        isMoving = Mathf.Abs(moveInput.x) > 0.1f;
    }

    private void FixedUpdate()
    {
        // 1. RECORD: Save the velocity at the start of the physics frame for accurate bouncing
        lastFrameVelocity = rb.linearVelocity;

        // 2. CALCULATE: Let the state machine do the math (updates zMomentum)
        currentState.UpdateState(this);

        // 3. GRAVITY: Apply vertical falling forces
        HandleGravity();

        // 4. EXECUTE: Construct the final velocity vector exactly ONCE and apply it.
        // We preserve whatever 'y' value gravity or jumping created, 
        // but force 'z' to equal our mathematically perfect zMomentum.
        Vector3 finalVelocity = rb.linearVelocity;
        finalVelocity.z = zMomentum;
        finalVelocity.x = 0f;

        rb.linearVelocity = finalVelocity;
    }

    private void InitializeStates()
    {
        stateCache.Add(typeof(IdleState), new IdleState());
        stateCache.Add(typeof(WalkingState), new WalkingState());
        stateCache.Add(typeof(JumpingState), new JumpingState());

        currentState = stateCache[typeof(IdleState)];
        currentState.EnterState(this);
    }

    public void ChangeState<T>() where T : IState
    {
        if (currentState.GetType() == typeof(T)) return;

        currentState.ExitState(this);
        currentState = stateCache[typeof(T)];
        currentState.EnterState(this);
    }

    public bool CheckJumpInput()
    {
        return InputManager.Instance.ConsumeJumpPressed();
    }

    public void Movement()
    {
        // Notice how there are no rb.linearVelocity assignments here anymore.
        // This function purely calculates "zMomentum" and trusts FixedUpdate to apply it.

        float inputX = moveInput.x;

        if (Mathf.Abs(inputX) > 0.1f)
        {
            float direction = Mathf.Sign(inputX);
            float initialSpeed = Mathf.Min(startSpeed, targetReachSpeed);

            if (Mathf.Abs(zMomentum) < 0.01f || Mathf.Sign(zMomentum) != direction)
            {
                zMomentum = direction * initialSpeed;
            }

            float baseAccelerationRate = Mathf.Abs(targetReachSpeed - initialSpeed) / Mathf.Max(0.001f, accelerationTime);
            float accelerationRate = baseAccelerationRate * Mathf.Max(0f, accelerationAmount);
            float targetZ = direction * targetReachSpeed;

            zMomentum = Mathf.MoveTowards(zMomentum, targetZ, accelerationRate * Time.fixedDeltaTime);
        }
        else
        {
            float baseDecelerationRate = targetReachSpeed / Mathf.Max(0.001f, decelerationTime);
            float decelerationRate = baseDecelerationRate * Mathf.Max(0f, decelerationAmount);

            zMomentum = Mathf.MoveTowards(zMomentum, 0f, decelerationRate * Time.fixedDeltaTime);
        }
    }

    public void HandleGravity()
    {
        Vector3 gravity = Physics.gravity * gravityScale;

        if (rb.linearVelocity.y < 0.1f)
        {
            gravity *= fallMultiplier;
        }

        rb.AddForce(gravity, ForceMode.Acceleration);

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            Vector3 clampedVelocity = rb.linearVelocity;
            clampedVelocity.y = -maxFallSpeed;
            rb.linearVelocity = clampedVelocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((wallMask.value & (1 << collision.gameObject.layer)) > 0)
        {
            Bounce(collision);
        }
    }

    private void Bounce(Collision collision)
    {
        if (!groundCheckerForBounce) return;
        Vector3 normal = collision.contacts[0].normal;

        // Ignore literal floor/ceiling hits
        if (Mathf.Abs(normal.y) > 0.5f) return;

        // Use the velocity recorded BEFORE the wall inevitably stopped the Rigidbody
        Vector3 incomingVelocity = lastFrameVelocity;
        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);

        if (incomingVelocity.magnitude > minBounceSpeed || Mathf.Abs(incomingVelocity.z) > 2f)
        {
            Vector3 finalBounce = reflectedVelocity * bounceSpeedMultiplier;

            // Apply upward logic for Icy Tower feel
            float upwardBounceForce = jumpForce * 1.2f;
            finalBounce.y = upwardBounceForce;

            rb.linearVelocity = finalBounce;

            // Overwrite momentum tracker so Movement doesn't instantly fight the bounce
            zMomentum = finalBounce.z;

            // Switch to Jumping state automatically
            ChangeState<JumpingState>();
        }

        groundCheckerForBounce = false;
    }

    public void Jump()
    {
        // Instantaneous vertical forces can instantly overwrite rb.linearVelocity.y
        // without affecting the horizontal zMomentum pipeline.
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    public void InAirMovement()
    {
        float inputX = moveInput.x;

        if (Mathf.Abs(inputX) > 0.1f)
        {
            float direction = Mathf.Sign(inputX);
            float targetZ = direction * maxAirSpeed;

            // FIX: If we are starting from zero or changing directions in mid-air, 
            // we need to give a small speed boost and snap the turnaround faster!
            if (Mathf.Abs(zMomentum) < 0.01f || Mathf.Sign(zMomentum) != direction)
            {
                // Instantly bump them to startSpeed, just like the ground, but allow 
                // a 2x faster acceleration to snap their direction safely.
                if (Mathf.Abs(zMomentum) < 0.01f) zMomentum = direction * startSpeed;

                float airTurnSpeed = airAcceleration * 2.5f;
                zMomentum = Mathf.MoveTowards(zMomentum, targetZ, airTurnSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // Standard air acceleration
                zMomentum = Mathf.MoveTowards(zMomentum, targetZ, airAcceleration * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Slowly drift
            zMomentum = Mathf.MoveTowards(zMomentum, 0f, airDeceleration * Time.fixedDeltaTime);
        }
    }

    public bool IsGrounded()
    {
        bool grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (grounded)
        {
            groundCheckerForBounce = true;
        }
        return grounded;
    }

    private void GroundCheckForBounce()
    {

    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        Vector3 end = origin + direction * groundCheckDistance;

        bool grounded = Physics.Raycast(
            origin, direction, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawRay(origin, direction * groundCheckDistance);
        Gizmos.DrawWireSphere(end, 0.03f);
    }

    private void OnGUI()
    {
        if (!showCurrentStateOnScreen || currentState == null) return;

        if (stateLabelStyle == null)
        {
            stateLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        // 1. Current State
        GUI.Label(new Rect(10f, 10f, 500f, 24f), "State: " + currentState.GetType().Name, stateLabelStyle);

        // 2. Linear Velocity (Shows X, Y, Z of the actual physics engine)
        if (rb != null)
        {
            GUI.Label(new Rect(10f, 34f, 500f, 24f), "Velocity: " + rb.linearVelocity.ToString("F2"), stateLabelStyle);
        }

        // 3. Built-up Momentum Tracker (Your manual Z variable)
        GUI.Label(new Rect(10f, 58f, 500f, 24f), "zMomentum: " + zMomentum.ToString("F3"), stateLabelStyle);
    }
}