using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    
    public GameObject Settings;
    private PlayerScript playerScript;
    public Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerScript = GetComponent<PlayerScript>();
        button.onClick.AddListener(OnToggleMenu);

        button.gameObject.SetActive(playerScript.IsUsingMobileControls());
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void OnToggleMenu()
    {
        if (Settings.activeSelf == true){
            if (playerScript != null && playerScript.IsUsingMobileControls())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Settings.SetActive(false);
            if (playerScript != null)
            {
                playerScript.SetCanMove(true);
            }
        }
        
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Settings.SetActive(true);
            if (playerScript != null)
            {
                playerScript.SetCanMove(false);
            }
            
        }
    }

    public void OpenDonationPage()
    {
        Application.OpenURL("");
    }
}
