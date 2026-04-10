using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    const string PrefPlayerSpeed = "settings.player.speed";
    const string PrefCameraSensitivity = "settings.player.sensitivity";
    const string PrefCameraFov = "settings.player.fov";
    const string PrefRenderDistance = "settings.player.renderDistance";

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

    public Slider speedSlider;
    public Slider senseSlider;
    public Slider fovSlider;
    public Slider renderSlider;
    
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

        LoadSavedSettings();
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
        SetPlayerSpeed(PlayerSpeedChange, true);
    }

    void SetPlayerSpeed(float PlayerSpeedChange, bool savePreference)
    {
        PlayerSpeed = PlayerSpeedChange;
        if (CurrentSpeedDisplay != null)
        {
            CurrentSpeedDisplay.text = PlayerSpeed.ToString();
        }

        if (speedSlider != null)
        {
            speedSlider.SetValueWithoutNotify(PlayerSpeed);
        }

        if (savePreference)
        {
            PlayerPrefs.SetFloat(PrefPlayerSpeed, PlayerSpeed);
            PlayerPrefs.Save();
        }

    }

    public void SetPlayerSense(float PlayerSenseChange)
    {
        SetPlayerSense(PlayerSenseChange, true);
    }

    void SetPlayerSense(float PlayerSenseChange, bool savePreference)
    {
        CameraSense = PlayerSenseChange;
        if (CurrentSenseDisplay != null)
        {
            CurrentSenseDisplay.text = CameraSense.ToString("F1");
        }

        if (senseSlider != null)
        {
            senseSlider.SetValueWithoutNotify(CameraSense);
        }

        if (savePreference)
        {
            PlayerPrefs.SetFloat(PrefCameraSensitivity, CameraSense);
            PlayerPrefs.Save();
        }
    }

    public void SetFovValue(float PlayerFovChange)
    {
        SetFovValue(PlayerFovChange, true);
    }

    void SetFovValue(float PlayerFovChange, bool savePreference)
    {
        if (Camera != null)
        {
            Camera.fieldOfView = PlayerFovChange;
        }

        if (CurrentFovDisplay != null && Camera != null)
        {
            CurrentFovDisplay.text = ((int)Camera.fieldOfView).ToString();
        }

        if (fovSlider != null)
        {
            fovSlider.SetValueWithoutNotify(PlayerFovChange);
        }

        if (savePreference)
        {
            PlayerPrefs.SetFloat(PrefCameraFov, PlayerFovChange);
            PlayerPrefs.Save();
        }
    }

    public void SetRenderValue(float PlayerRenderChange)
    {
        SetRenderValue(PlayerRenderChange, true);
    }

    void SetRenderValue(float PlayerRenderChange, bool savePreference)
    {
        if (Camera != null)
        {
            Camera.farClipPlane = PlayerRenderChange;
        }

        if (CurrentRenderDisplay != null && Camera != null)
        {
            CurrentRenderDisplay.text = Camera.farClipPlane.ToString();
        }

        if (renderSlider != null)
        {
            renderSlider.SetValueWithoutNotify(PlayerRenderChange);
        }

        if (savePreference)
        {
            PlayerPrefs.SetFloat(PrefRenderDistance, PlayerRenderChange);
            PlayerPrefs.Save();
        }
    }

    void LoadSavedSettings()
    {
        float savedSpeed = PlayerPrefs.GetFloat(PrefPlayerSpeed, PlayerSpeed);
        float savedSensitivity = PlayerPrefs.GetFloat(PrefCameraSensitivity, CameraSense);
        float savedFov = PlayerPrefs.GetFloat(PrefCameraFov, Camera != null ? Camera.fieldOfView : 60f);
        float savedRenderDistance = PlayerPrefs.GetFloat(PrefRenderDistance, Camera != null ? Camera.farClipPlane : 1000f);

        // Load once at startup without re-saving immediately.
        SetPlayerSpeed(savedSpeed, false);
        SetPlayerSense(savedSensitivity, false);
        SetFovValue(savedFov, false);
        SetRenderValue(savedRenderDistance, false);
    }


    
    
    
    
}
