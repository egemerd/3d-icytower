using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour, IStateMachine
{
    private InputManager input;
    private Vector2 moveInput;
    private Rigidbody rb;
    private IState currentState;
    [SerializeField] public Animator animator;
    public PlayerAttack playerAttack { get; private set; }

    private Dictionary<System.Type, IState> stateCache = new Dictionary<System.Type, IState>();
    [Header("References")]
    [SerializeField] private Transform characterModel;

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

    [Header("Jump Spin (Icy Tower Effect)")]
    [SerializeField] private float minUpwardSpeedForSpin = 5f;
    [SerializeField] private float jumpSpinSpeed = 1200f; // Saniyede kaç derece döneceði
    private float currentZRotation = 0f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Wall Bounce")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float minBounceSpeed = 5f;
    [SerializeField] private float bounceSpeedMultiplier = 1.0f;
    [SerializeField] private bool groundCheckerForBounce = true;
    [SerializeField] private float bounceJumpForce = 2f;

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

    [Header("Effects")]
    [SerializeField] private ParticleSystem walkingEffect;

    [Header("Debug")]
    [SerializeField] private bool showCurrentStateOnScreen = true;

    [Header("Rotation")]
    private float rightFacingAngle = 90f;
    private float leftFacingAngle = -90f;
    private float rotationSpeed = 15f;

    private float targetYRotation = 90f;

    private GUIStyle stateLabelStyle;
    public Rigidbody Rb => rb;

    public float MantleBoostTimer => mantleBoostTimer;


    private Vector3 lastFrameVelocity; //tracker for bounce calculations, recorded at the start of each FixedUpdate before any physics changes it.
    public float zMomentum { get; private set; }
    public Vector3 mantlePosition { get; private set; }
    public Vector3 mantleStartPosition { get; private set; }

    public bool isMoving { get; private set; }
    public bool isMantling { get; private set; }
    public bool isRocketActive { get; set; }
    public bool isAttacking { get; set; }

    public void UnlockFromMantle()
    {
        isMantling = false;
    }

    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerAttack = GetComponent<PlayerAttack>();
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

        HandleRotation();
        RotateCharacter();

        if (isMantling)
        {
            rb.linearVelocity = Vector3.zero;
            currentState.UpdateState(this);
            return; 
        }
        if (isAttacking)
        {
            currentState.UpdateState(this);
            return;
        }

        


        HandleWalkEffect();

        CheckForMantleOverlap();
        
        lastFrameVelocity = rb.linearVelocity; //Save the velocity at the start of the physics frame for accurate bouncing

        currentState.UpdateState(this);

        HandleGravity();

        //Construct the final velocity vector exactly ONCE and apply it.
        //We preserve whatever 'y' value gravity or jumping created, 
        //but force 'z' to equal our mathematically perfect zMomentum.
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
        stateCache.Add(typeof(AttackingState), new AttackingState());

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
    public void SetZMomentum(float newMomentum)
    {
        zMomentum = newMomentum;
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


    private void HandleRotation()
    {
        float inputX = moveInput.x;

        // 1. Only update the target direction if the player is pressing a button
        if (Mathf.Abs(inputX) > 0.1f)
        {
            targetYRotation = inputX > 0 ? rightFacingAngle : leftFacingAngle;
        }

        // 2. ALWAYS smoothly rotate towards our target, even when inputX is 0
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    public void ResetRotationToForward()
    {
        // Sets the target rotation to 0 (facing the original start direction).
        // The existing HandleRotation() method will automatically and smoothly Slerp us there.
        targetYRotation = 0f;
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
            if (isRocketActive)
                BounceRocket(collision);  // Unlimited billiard bounce during ulti
            else
                Bounce(collision);        // Original single bounce
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

        // 1. Velocity Check: You must be moving faster than the minBounceSpeed to trigger a bounce.
        if (incomingVelocity.magnitude < minBounceSpeed )
        {
            return;
        }

        // 2. Input Direction Check: Prevent bouncing if the player is holding the key INTO the wall.
        
            float inputDirection = Mathf.Sign(moveInput.x);
            float wallFacingDirection = Mathf.Sign(normal.z); // normal.z points AWAY from the wall

            // If the input direction is opposite to the normal, the player is pressing INTO the wall.
        

        // 3. Execute the bounce
        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);
        Vector3 finalBounce = reflectedVelocity * bounceSpeedMultiplier;

        // Apply upward logic for Icy Tower feel
        float upwardBounceForce = jumpForce * bounceJumpForce;
        finalBounce.y = upwardBounceForce;

        rb.linearVelocity = finalBounce;

        // Overwrite momentum tracker so Movement doesn't instantly fight the bounce
        zMomentum = finalBounce.z;

        // Switch to Jumping state automatically
        ChangeState<JumpingState>();

        groundCheckerForBounce = false;
    }

    private void BounceRocket(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;

        // Flatten the normal to the Z/Y plane only.
        // Your game has no real X movement — keeping X in the normal
        // would produce a reflection with an X component that the
        // pipeline would then zero out, breaking the angle.
        normal.x = 0f;
        if (normal.sqrMagnitude < 0.001f) return; // degenerate hit, ignore
        normal = normal.normalized;

        // Read current velocity and flatten to Z/Y as well.
        // We use rb.linearVelocity directly here (not lastFrameVelocity)
        // because OnCollisionEnter fires in the same physics step as the
        // impact — the velocity is still the incoming velocity at this point.
        Vector3 incoming = rb.linearVelocity;
        incoming.x = 0f;

        float speed = incoming.magnitude;
        if (speed < 0.1f) speed = 1f; // fallback so we never reflect a zero vector

        // Pure geometric reflection in the Z/Y plane.
        // dot = how much of the incoming direction aligns with the wall normal.
        // reflect = v - 2(v·n)n  — this is the billiard formula.
        float dot = Vector3.Dot(incoming.normalized, normal);
        Vector3 reflected = incoming.normalized - 2f * dot * normal;

        // Preserve exact pre-impact speed — no gain, no loss.
        Vector3 finalVelocity = reflected.normalized * speed;
        finalVelocity.x = 0f; // belt-and-suspenders: guarantee no X drift

        // Write velocity and sync zMomentum in one atomic step.
        // RocketUltiHandler.FixedUpdate reads rb.linearVelocity.normalized
        // next physics frame, so the direction is already correct and it
        // simply scales it to currentSpeed.
        rb.linearVelocity = finalVelocity;
        zMomentum = finalVelocity.z;

    }


    public void Jump()
    {
        // Instantaneous vertical forces can instantly overwrite rb.linearVelocity.y
        // without affecting the horizontal zMomentum pipeline.
        animator.SetTrigger("Jump");

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        
    }

    public void InAirMovement()
    {
        Movement();
    }
    public void RotateCharacter()
    {

        if (rb.linearVelocity.y > minUpwardSpeedForSpin && !IsGrounded())
        {
            // Ýleri gidiyorsa ileri, geri gidiyorsa geriye doðru takla (spin) atsýn
            float direction = zMomentum >= 0 ? -1f : 1f;
            currentZRotation += jumpSpinSpeed * direction * Time.deltaTime;
        }
        else
        {
            // Yukarý hýzý azaldýðýnda veya yere düþtüðünde pürüzsüzce düz duruþa geri döner
            currentZRotation = Mathf.LerpAngle(currentZRotation, 0f, 15f * Time.deltaTime);
        }

        // ÇÖZÜM: "*=" (çarpý eþittir) yerine doðrudan "=" (eþittir) kullanýyoruz ve localRotation'a atýyoruz.
        // Eðer modelinizin taklasý yanlýþ eksende atýyorsa (X yerine Z ekseni gerekliyse):
        // Quaternion.Euler(0, 0, currentZRotation) olarak deðiþtirebilirsiniz.
        characterModel.localRotation = Quaternion.Euler(0, 0,currentZRotation) ;
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

    
    public void PlayerAttackUndamagableEnter()
    {
        GetComponent<PlayerHealth>().canTakeDamage = false;

    }
    public void PlayerAttackUndamagableExit()
    {
        GetComponent<PlayerHealth>().canTakeDamage = true;
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

    private void HandleWalkEffect()
    {
        if(currentState.GetType() == typeof(WalkingState))
        {
            walkingEffect.Play();
        }
        else
        {
            walkingEffect.Stop();
        }
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


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Chunk") && rb.linearVelocity.y < 0.1f)
        {
            Debug.LogError("Player Died.");
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