using System.Collections;
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
    private Coroutine faceRoutine;
    private bool isAutoFacing;
    // Set when a turn should also stop the player; cleared once movement input is released.
    private bool waitForMoveRelease;
    
    
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

        if (waitForMoveRelease)
        {
            if (MoveDirection.sqrMagnitude < 0.01f)
            {
                waitForMoveRelease = false;
            }
            else
            {
                MoveDirection = Vector2.zero;
            }
        }

        if (CanMove == true)
        {
            Vector3 CombinedTransform = transform.forward * MoveDirection.y + transform.right * MoveDirection.x;

            rb.linearVelocity = CombinedTransform * PlayerSpeed;

            // FaceTowards drives the view while it runs; applying look input too would fight it.
            if (!isAutoFacing)
            {
                transform.Rotate(0, LookDirection.x * CameraSense, 0 , Space.Self);
                cameraPitch -= LookDirection.y * CameraSense;
                cameraPitch = Mathf.Clamp(cameraPitch, angleClamp.x, angleClamp.y);
                Camera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
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

    public bool IsLookingAt(Vector3 worldPoint, float toleranceDegrees)
    {
        return Vector3.Angle(Camera.transform.forward, worldPoint - Camera.transform.position) <= toleranceDegrees;
    }

    // Smoothly turns the view toward a world point. A positive verticalOffsetDegrees tilts
    // the view further down, which raises the point on screen (used to lift a panel above
    // the on-screen keyboard). With stopMoving, a held joystick no longer walks the player
    // until it is released; otherwise, after the turn, "forward" would carry them straight
    // through the panel they were just turned to face.
    public void FaceTowards(Vector3 worldPoint, float verticalOffsetDegrees = 0f, float duration = 0.6f,
                            bool stopMoving = false)
    {
        if (stopMoving)
        {
            waitForMoveRelease = true;
        }

        if (faceRoutine != null)
        {
            StopCoroutine(faceRoutine);
        }

        faceRoutine = StartCoroutine(FaceTowardsRoutine(worldPoint, verticalOffsetDegrees, duration));
    }

    IEnumerator FaceTowardsRoutine(Vector3 target, float verticalOffsetDegrees, float duration)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
        {
            faceRoutine = null;
            yield break;
        }

        Quaternion startYaw = transform.rotation;
        Quaternion endYaw = Quaternion.LookRotation(flat.normalized, Vector3.up);

        // Positive pitch looks down, so a target above the camera needs a negative pitch.
        Vector3 fromCamera = target - Camera.transform.position;
        float horizontal = new Vector2(fromCamera.x, fromCamera.z).magnitude;
        float endPitch = -Mathf.Atan2(fromCamera.y, horizontal) * Mathf.Rad2Deg + verticalOffsetDegrees;
        endPitch = Mathf.Clamp(endPitch, angleClamp.x, angleClamp.y);
        float startPitch = cameraPitch;

        isAutoFacing = true;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.rotation = Quaternion.Slerp(startYaw, endYaw, blend);
            cameraPitch = Mathf.Lerp(startPitch, endPitch, blend);
            Camera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            yield return null;
        }

        transform.rotation = endYaw;
        cameraPitch = endPitch;
        Camera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        isAutoFacing = false;
        faceRoutine = null;
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
