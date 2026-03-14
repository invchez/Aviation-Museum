using UnityEngine;

public class SettingsScript : MonoBehaviour
{
    
    public GameObject Settings;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnToggleMenu()
    {
        if (Settings.activeSelf == true){
            Cursor.lockState=CursorLockMode.Locked;
            Settings.SetActive(false);
            GetComponent<PlayerScript>().SetCanMove(true);
        }
        
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Settings.SetActive(true);
            GetComponent<PlayerScript>().SetCanMove(false);
            
        }
    }

    public void OpenDonationPage()
    {
        Application.OpenURL("");
    }
}
