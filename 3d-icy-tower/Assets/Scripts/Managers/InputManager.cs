using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input System")]
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;

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
        Debug.Log($"Move Input: {moveInput}, Is Moving: {isMoving}");
    }

}
