using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private float maxAirSpeed = 12f;
    [SerializeField] private float airAcceleration = 8f;   // Yavaþ hýzlanma
    [SerializeField] private float airTurnSpeed = 15f;      // Havada dönüþ keskinliði
    [SerializeField] private float airFriction = 0.5f;

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

    [Header("Mantling")]
    [SerializeField] private float mantleJumpBoost = 20f;
    [SerializeField] private float mantleBoostTimer = 0.5f;
    [SerializeField] private float mantleDuration = 1f;
    [SerializeField] private float mantleOffset = 1.2f;

    [Header("Mantling (OverlapSphere logic)")]
    [SerializeField] private float mantleGrabRadius = 0.5f; // How big is the grab area?
    [SerializeField] private float mantleGrabHeight = 0.8f; // How high up on the body are the hands?
    [SerializeField] private float mantleGrabForward = 0.5f; // How far forward from the body are the hands?
    [SerializeField] private LayerMask mantleMask;



    [Header("Gravity")]
    [SerializeField] private float gravityScale = 2.5f;
    [SerializeField] private float fallMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = 40f;

    [Header("Debug")]
    [SerializeField] private bool showCurrentStateOnScreen = true;

    private GUIStyle stateLabelStyle;
    public Rigidbody Rb => rb;

    public float MantleBoostTimer => mantleBoostTimer;


    private Vector3 lastFrameVelocity; //tracker for bounce calculations, recorded at the start of each FixedUpdate before any physics changes it.
    private float zMomentum;
    public Vector3 mantlePosition { get; private set; }
    public Vector3 mantleStartPosition { get; private set; }

    public bool isMoving { get; private set; }
    public bool isMantling { get; private set; }

    public void UnlockFromMantle()
    {
        isMantling = false;
    }

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
        if (isMantling)
        {
            rb.linearVelocity = Vector3.zero;
            currentState.UpdateState(this);
            return; // Skip all physics, gravity, and movement updates
        }

        CheckForMantleOverlap();
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
        stateCache.Add(typeof(MantleState), new MantleState());

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
        Movement();
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

    public void MantleBoostJump()
    {
        Debug.Log("Mantle Boost Jump Activated!");
        // 1. Get input direction (-1, 0, or 1)
        float faceDirection = Mathf.Abs(moveInput.x) > 0.1f ? Mathf.Sign(moveInput.x) : 1f;

        // 2. Erase any zero'd out velocity
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        // 3. Apply massive vertical boost
        rb.AddForce(Vector3.up * mantleJumpBoost, ForceMode.VelocityChange);

        // 4. Overwrite zMomentum to give them a massive forward speed boost as well
        // We multiply maxAirSpeed by a boost factor to make it feel explosive
        zMomentum = faceDirection * (maxAirSpeed * 1.5f);
    }

    public void MantleNormalJump()
    {
        Debug.Log("Mantle Jump Activated!");
        // Same as boost jump, but weaker. Or you can just call your standard jump!
        float faceDirection = Mathf.Abs(moveInput.x) > 0.1f ? Mathf.Sign(moveInput.x) : 1f;

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        // Uses standard jump force instead of the massive mantleJumpBoost
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        // Just start them at normal air speed
        zMomentum = faceDirection * maxAirSpeed;
    }

    private void CheckForMantleOverlap()
    {
        // Don't mantle if we are already mantling or standing safely on the ground
        if (isMantling || IsGrounded()) return;

        // Calculate where the "hands" are. 
        // We move UP from the feet by grabHeight, and FORWARD based on the player's facing direction or momentum
        float faceDirection = Mathf.Abs(moveInput.x) > 0.1f ? Mathf.Sign(moveInput.x) : Mathf.Sign(zMomentum);
        if (Mathf.Abs(faceDirection) < 0.1f) faceDirection = 1f; // Default facing

        Vector3 handPosition = transform.position + (Vector3.up * mantleGrabHeight) + (new Vector3(0, 0, faceDirection) * mantleGrabForward);

        // Instantly generate a sphere at handPosition. Returns an array of all colliders it touches.
        Collider[] hits = Physics.OverlapSphere(handPosition, mantleGrabRadius, mantleMask);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("MantleCollider"))
            {
                // We found a ledge! Calculate where we need to climb to.
                mantlePosition = hit.ClosestPoint(transform.position) + (Vector3.up * mantleOffset);
                mantleStartPosition = transform.position; // Start climbing from where we are currently at

                ChangeState<MantleState>();
                return; // Stop checking, we found one!
            }
        }
    }

    public void CallMantleJumpCoroutine()
    {
        StartCoroutine(MantleCoroutine(mantlePosition));    
    }

    

    private IEnumerator MantleCoroutine(Vector3 targetPosition)
    {
        isMantling = true;
        zMomentum = 0f;

        float elapsed = 0f;
        float duration = mantleDuration;

        // FIX: Start exactly where the player's center body is right now, no teleporting!
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            // Smoothly move from current body position to the calculated top position
            rb.MovePosition(Vector3.Lerp(startPosition, targetPosition, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        isMantling = false;
        ChangeState<IdleState>();
        yield return null;

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

        if (Application.isPlaying) // We need gameplay momentum/input to know which way we are checking
        {
            float faceDirection = Mathf.Abs(moveInput.x) > 0.1f ? Mathf.Sign(moveInput.x) : Mathf.Sign(zMomentum);
            if (Mathf.Abs(faceDirection) < 0.1f) faceDirection = 1f;

            Vector3 handPosition = transform.position + (Vector3.up * mantleGrabHeight) + (new Vector3(0, 0, faceDirection) * mantleGrabForward);

            Gizmos.color = new Color(0, 1, 1, 0.5f); // Semi-transparent Cyan
            Gizmos.DrawSphere(handPosition, mantleGrabRadius);
        }
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