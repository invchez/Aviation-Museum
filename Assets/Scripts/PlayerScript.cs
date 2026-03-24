using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;

public class PlayerScript : MonoBehaviour
{

    Vector2 MoveDirection;
    Vector2 LookDirection;
    public float CameraSense;
    public float PlayerSpeed;
    public Vector2 angleClamp = new Vector2(-60, 70);
    Rigidbody rb;
    Camera Camera;
    float cameraPitch;
    LayerMask layerMask;
    private bool CanMove = true;
    public TextMeshProUGUI CurrentSpeedDisplay;
    public TextMeshProUGUI CurrentSenseDisplay;
    public TextMeshProUGUI CurrentFovDisplay;
    public TextMeshProUGUI CurrentRenderDisplay;
    
    public FixedJoystick movementJoystick;
    [Header("Mobile Controls")]
    [SerializeField] private bool forceMobileModeForDebug;
    [SerializeField, Range(0.4f, 0.9f)] private float rightSideLookStart = 0.55f;
    [SerializeField] private float swipeLookSensitivity = 0.08f;
    private bool useMobileControls;
    private int activeLookTouchId = -1;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Camera = GetComponentInChildren<Camera>();
        cameraPitch = Camera.transform.localEulerAngles.x;
        if (cameraPitch > 180f)
        {
            cameraPitch -= 360f;
        }
        ApplyControlMode(IsMobileModeEnabled());
        layerMask = LayerMask.GetMask("UI");
        
        SetPlayerSense(CameraSense);
    }

    // Update is called once per frame
    void Update()
    {
        bool shouldUseMobileControls = IsMobileModeEnabled();
        if (shouldUseMobileControls != useMobileControls)
        {
            ApplyControlMode(shouldUseMobileControls);
        }

        if (useMobileControls)
        {
            UpdateMobileInput();
        }

        if (CanMove == true)
        {
            Vector3 CombinedTransform = transform.forward * MoveDirection.y + transform.right * MoveDirection.x;

            rb.linearVelocity = CombinedTransform * PlayerSpeed;
            transform.Rotate(0, LookDirection.x * CameraSense, 0 , Space.Self);
            cameraPitch -= LookDirection.y * CameraSense;
            cameraPitch = Mathf.Clamp(cameraPitch, angleClamp.x, angleClamp.y);
            Camera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
        
    }

    void LateUpdate()
    {
        // Keep cursor free in mobile mode even if other scripts change it.
        if (!useMobileControls)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    bool IsMobileModeEnabled()
    {
        return Application.isMobilePlatform || forceMobileModeForDebug;
    }

    void ApplyControlMode(bool mobileModeEnabled)
    {
        useMobileControls = mobileModeEnabled;
        MoveDirection = Vector2.zero;
        LookDirection = Vector2.zero;
        activeLookTouchId = -1;

        if (movementJoystick != null)
        {
            movementJoystick.gameObject.SetActive(useMobileControls);
        }

        Cursor.lockState = useMobileControls ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = useMobileControls;
    }

    void UpdateMobileInput()
    {
        if (movementJoystick != null)
        {
            MoveDirection = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);
        }
        else
        {
            MoveDirection = Vector2.zero;
        }

        LookDirection = GetMobileLookInput();
    }

    Vector2 GetMobileLookInput()
    {
        if (Touchscreen.current == null)
        {
            activeLookTouchId = -1;
            return Vector2.zero;
        }

        float lookZoneStartX = Screen.width * rightSideLookStart;
        Vector2 lookDelta = Vector2.zero;
        bool foundLookTouch = false;

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
            {
                continue;
            }

            int touchId = touch.touchId.ReadValue();
            Vector2 touchPosition = touch.position.ReadValue();

            if (activeLookTouchId == -1 &&
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began &&
                touchPosition.x >= lookZoneStartX)
            {
                activeLookTouchId = touchId;
            }

            if (touchId != activeLookTouchId)
            {
                continue;
            }

            lookDelta = touch.delta.ReadValue() * swipeLookSensitivity;
            foundLookTouch = true;
            break;
        }

        if (!foundLookTouch)
        {
            activeLookTouchId = -1;
        }

        return lookDelta;
    }

    public void SetCanMove(bool Move)
    {
        CanMove = Move;
    }

    public bool IsUsingMobileControls()
    {
        return useMobileControls;
    }

    void OnMove(InputValue Action)
    {
        if (useMobileControls)
        {
            return;
        }

        MoveDirection = Action.Get<Vector2>();

    }

    void OnLook(InputValue Action)
    {
        if (useMobileControls)
        {
            return;
        }

        LookDirection = Action.Get<Vector2>();
    }

    public void SetPlayerSpeed(float PlayerSpeedChange)
    {
        PlayerSpeed = PlayerSpeedChange;
        CurrentSpeedDisplay.text = PlayerSpeed.ToString();
        

    }

    public void SetPlayerSense(float PlayerSenseChange)
    {
        CameraSense = PlayerSenseChange;
        CurrentSenseDisplay.text = CameraSense.ToString("F1");
    }

    public void SetFovValue(float PlayerFovChange)
    {
        Camera.fieldOfView = PlayerFovChange;
        CurrentFovDisplay.text = ((int)Camera.fieldOfView).ToString();
    }

    public void SetRenderValue(float PlayerRenderChange)
    {
        Camera.farClipPlane = PlayerRenderChange;
        CurrentRenderDisplay.text = Camera.farClipPlane.ToString();
    }


    
    
    
    
}
