using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input System")]
    private PlayerInput playerInput;

    private InputAction moveAction;
    public InputAction jumpAction;

    public bool jumpPressed;

    public Vector2 moveInput { get; private set; }

    //Booleans
    public bool isMoving;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerInput = GetComponent<PlayerInput>();
        InitializeActions();
    }

    private void InitializeActions()
    {
        moveAction = playerInput.actions.FindAction("Move");
        jumpAction = playerInput.actions.FindAction("Jump");
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        isMoving = moveInput.magnitude > 0.1f;
        if (jumpAction.WasPressedThisFrame())
        {
            jumpPressed = true;
        }
    }

    public bool ConsumeJumpPressed()
    {
        bool wasPressed = jumpPressed;
        jumpPressed = false;
        return wasPressed;
    }
}
