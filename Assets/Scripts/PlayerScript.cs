using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
        else
        {
            UpdateDesktopInput();
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

    public bool IsMobileModeEnabled()
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

    void UpdateDesktopInput()
    {
        Vector2 desktopMove = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                desktopMove.y += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                desktopMove.y -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                desktopMove.x += 1f;
            }

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                desktopMove.x -= 1f;
            }
        }

        MoveDirection = Vector2.ClampMagnitude(desktopMove, 1f);

        if (Mouse.current != null)
        {
            LookDirection = Mouse.current.delta.ReadValue();
        }
        else
        {
            LookDirection = Vector2.zero;
        }
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

    public void ApplyPlayerSpeed(float PlayerSpeedChange)
    {
        PlayerSpeed = PlayerSpeedChange;
    }

    public void ApplyCameraSensitivity(float PlayerSenseChange)
    {
        CameraSense = PlayerSenseChange;
    }

    public void ApplyCameraFov(float PlayerFovChange)
    {
        if (Camera != null)
        {
            Camera.fieldOfView = PlayerFovChange;
        }
    }

    public void ApplyRenderDistance(float PlayerRenderChange)
    {
        if (Camera != null)
        {
            Camera.farClipPlane = PlayerRenderChange;
        }
    }

    public float GetCameraFov()
    {
        return Camera != null ? Camera.fieldOfView : 60f;
    }

    public float GetRenderDistance()
    {
        return Camera != null ? Camera.farClipPlane : 1000f;
    }


    
    
    
    
}
