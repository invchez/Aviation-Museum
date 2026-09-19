using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    // The Enter and About buttons are laid out as a matched pair in the scene. On mobile
    // the Exit and Donate buttons are removed: iOS apps must not offer their own quit
    // control, and there is no donation flow in the app.
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject donateButton;

    void Start()
    {
        if (!Application.isMobilePlatform)
        {
            return;
        }

        SetActive(exitButton, "ExitButton", false);
        SetActive(donateButton, "Donate Button", false);
    }

    void SetActive(GameObject assigned, string fallbackName, bool active)
    {
        GameObject target = assigned != null ? assigned : GameObject.Find(fallbackName);
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    // Wired to the Exit button, which only exists in desktop builds.
    public void ExitApplication()
    {
        Application.Quit();
    }
}
