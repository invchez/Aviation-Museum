using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerScript : MonoBehaviour
{

    Vector2 MoveDirection;
    Vector2 LookDirection;
    public float CameraSense;
    public float PlayerSpeed;
    Rigidbody rb;
    Camera Camera;
    LayerMask layerMask;
    private bool CanMove = true;
    public TextMeshProUGUI CurrentSpeedDisplay;
    public TextMeshProUGUI CurrentSenseDisplay;
    public TextMeshProUGUI CurrentFovDisplay;
    public TextMeshProUGUI CurrentRenderDisplay;

    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Camera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        layerMask = LayerMask.GetMask("UI");
        
    }

    // Update is called once per frame
    void Update()
    {
        if (CanMove == true)
        {
            Vector3 CombinedTransform = transform.forward * MoveDirection.y + transform.right * MoveDirection.x;

            rb.linearVelocity = CombinedTransform * PlayerSpeed;
            transform.Rotate(0, LookDirection.x * CameraSense, 0 , Space.Self);
            Camera.transform.Rotate(-1 * LookDirection.y * CameraSense , 0 , 0);
        }
        
    }

    public void SetCanMove(bool Move)
    {
        CanMove = Move;
    }

    void OnMove(InputValue Action)
    {
        MoveDirection = Action.Get<Vector2>();

    }

    void OnLook(InputValue Action)
    {
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
        CurrentSenseDisplay.text = CameraSense.ToString();
    }

    public void SetFovValue(float PlayerFovChange)
    {
        Camera.fieldOfView = PlayerFovChange;
        CurrentFovDisplay.text = Camera.fieldOfView.ToString();
    }

    public void SetRenderValue(float PlayerRenderChange)
    {
        Camera.farClipPlane = PlayerRenderChange;
        CurrentRenderDisplay.text = Camera.farClipPlane.ToString();
    }


    
    
    
    
}
