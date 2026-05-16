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

    [Header("Skill Inputs")]
    [SerializeField] private string[] skillActionNames = { "Skill1", "Skill2", "Skill3", "Skill4" }; 
    private InputAction[] skillActions;

    public bool jumpPressed;
    public bool attackPressed;
    
    // NEW: Array to store the pressed state of each skill so FixedUpdate doesn't miss them
    public bool[] skillsPressed; 

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

    private void Start()
    {
        Debug.Log("skillActionNames.Length" + skillActionNames.Length);
        Debug.Log("skillActions.Length" + skillActions.Length);
    }
    
    private void InitializeActions()
    {
        moveAction = playerInput.actions.FindAction("Move");
        jumpAction = playerInput.actions.FindAction("Jump");
        attackAction = playerInput.actions.FindAction("Attack");

        skillActions = new InputAction[skillActionNames.Length];
        skillsPressed = new bool[skillActionNames.Length]; // Initialize the boolean array

        for(int i = 0; i < skillActionNames.Length; i++)
        {
            skillActions[i] = playerInput.actions.FindAction(skillActionNames[i]);
            if (skillActions[i] == null)
            {
                Debug.LogError($"Skill action not found: {skillActionNames[i]}");
            }
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
        
        if (attackAction != null && attackAction.WasPressedThisFrame())
        {
            attackPressed = true;
        }

        // CAPTURE SKILLS IN UPDATE
        if (skillActions != null)
        {
            for (int i = 0; i < skillActions.Length; i++)
            {
                if (skillActions[i] != null && skillActions[i].WasPressedThisFrame())
                {
                    skillsPressed[i] = true;
                }
            }
        }
    }

    public int GetSkillIndex()
    {
        for(int i = 0; i < skillsPressed.Length; i++)
        {
            if (skillsPressed[i])
            {
                return i; 
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

    public bool ConsumeSkillPressed(int index)
    {
        int arrayIndex = index - 1;

        if (arrayIndex < 0 || arrayIndex >= skillsPressed.Length)
            return false;

        // READ FROM THE BOOLEAN ARRAY DONT READ ACTION DIRECTLY
        bool wasPressed = skillsPressed[arrayIndex];
        
        if (wasPressed) 
        {
            skillsPressed[arrayIndex] = false; // Consume it
        }

        return wasPressed;
    }

    public bool ConsumeAttackPressed()
    {
        bool wasPressed = attackPressed;
        attackPressed = false; 
        return wasPressed;
    }
}
