using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialScript : MonoBehaviour
{
    const string PrefTutorialCompleted = "TutorialCompleted";

    // Keyboard and mouse tutorial, shown briefly on desktop.
    public CanvasGroup panel;
    public float startDelay = 2f;
    public float fadeTime = 1f;
    public float hideTime = 4;

    [Header("Touch")]
    // Touch tutorial. It stays on screen until dismissed, because a timed banner is
    // easy to miss while the visitor is still getting their bearings.
    public CanvasGroup touchPanel;
    public Button dismissButton;
    // HUD button that reopens the tutorial at any time.
    public Button helpButton;

    PlayerScript playerScript;

    void Start()
    {
        playerScript = FindFirstObjectByType<PlayerScript>();

        panel.gameObject.SetActive(false);
        if (touchPanel != null)
        {
            touchPanel.gameObject.SetActive(false);
        }

        if (dismissButton != null)
        {
            dismissButton.onClick.AddListener(HideTouchTutorial);
        }

        if (helpButton != null)
        {
            helpButton.onClick.AddListener(ShowTutorial);
            helpButton.gameObject.SetActive(UsesTouchTutorial());
        }

        if (PlayerPrefs.GetInt(PrefTutorialCompleted, 0) == 0)
        {
            StartCoroutine(StartShow());
        }
    }

    bool UsesTouchTutorial()
    {
        // IsMobileModeEnabled reads the platform directly; IsUsingMobileControls is only
        // set once PlayerScript.Start has run, and Start order across objects is not fixed.
        return touchPanel != null && playerScript != null && playerScript.IsMobileModeEnabled();
    }

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        print("Tutorial reset.");
        PlayerPrefs.SetInt(PrefTutorialCompleted, 0);
    }

    public void ShowTutorial()
    {
        StopAllCoroutines();

        if (UsesTouchTutorial())
        {
            ShowTouchTutorial();
        }
        else
        {
            StartCoroutine(FadeDesktopTutorial());
        }
    }

    IEnumerator StartShow()
    {
        yield return new WaitForSeconds(startDelay);

        if (UsesTouchTutorial())
        {
            ShowTouchTutorial();
            yield break;
        }

        yield return FadeDesktopTutorial();
    }

    void ShowTouchTutorial()
    {
        panel.gameObject.SetActive(false);
        touchPanel.alpha = 1f;
        touchPanel.gameObject.SetActive(true);

        // Hold the player still while they read, and hide the joystick, which would
        // otherwise draw over the card on the smaller screens.
        if (playerScript != null)
        {
            playerScript.SetCanMove(false);
            SetJoystickVisible(false);
        }
    }

    void SetJoystickVisible(bool visible)
    {
        if (playerScript.movementJoystick != null && playerScript.IsMobileModeEnabled())
        {
            playerScript.movementJoystick.gameObject.SetActive(visible);
        }
    }

    public void HideTouchTutorial()
    {
        if (touchPanel != null)
        {
            touchPanel.gameObject.SetActive(false);
        }

        if (playerScript != null)
        {
            playerScript.SetCanMove(true);
            SetJoystickVisible(true);
        }

        PlayerPrefs.SetInt(PrefTutorialCompleted, 1);
        PlayerPrefs.Save();
    }

    IEnumerator FadeDesktopTutorial()
    {
        panel.alpha = 0;
        panel.gameObject.SetActive(true);

        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            panel.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panel.alpha = 1;
        PlayerPrefs.SetInt(PrefTutorialCompleted, 1);

        yield return new WaitForSeconds(hideTime);

        elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            panel.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panel.alpha = 0;
        panel.gameObject.SetActive(false);
    }
}
