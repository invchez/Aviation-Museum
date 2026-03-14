using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnLook(InputValue action)
    {
        Debug.Log(action);
        Debug.Log(action.Get<Vector2>());
    }
}
