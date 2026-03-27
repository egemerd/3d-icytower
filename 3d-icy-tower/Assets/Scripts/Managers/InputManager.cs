using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input System")]
    private PlayerInput playerInput;

    private InputAction moveAction;
    public InputAction jumpAction;
    public InputAction attackAction;

    public InputAction skill1Action;
    public InputAction skill2Action;

    [Header("Skill Inputs")]
    [SerializeField] private string[] skillActionNames = { "Skill1", "Skill2" }; // Input Action Asset'indeki isimler
    private InputAction[] skillActions;

    public bool jumpPressed;
    public bool attackPressed;

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
        attackAction = playerInput.actions.FindAction("Attack");

        //Skill actionlarýný dinamik olarak bul ve sakla
        skillActions = new InputAction[skillActionNames.Length];
        for(int i = 0; i < skillActionNames.Length; i++)
        {
            skillActions[i] = playerInput.actions.FindAction(skillActionNames[i]);
        }
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

    public int GetSkillIndex()
    {
        for(int i = 0; i < skillActions.Length; i++)
        {
            if (skillActions[i].WasPressedThisFrame())
            {
                return i; // Hangi skill tuþuna basýldýðýný döndür
            }
        }
        return -1;
    }
    

    public bool ConsumeJumpPressed()
    {
        bool wasPressed = jumpPressed;
        jumpPressed = false;
        return wasPressed;
    }

    public bool ConsumeAttackPressed()
    {
        bool wasPressed = attackPressed;
        attackPressed = false; // Tüketildi, tekrar false yap!
        return wasPressed;
    }
}
