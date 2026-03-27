using System.Collections;
using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    public CanvasGroup panel;
    public float startDelay = 2f;
    public float fadeTime = 1f;
    public float hideTime = 4;

    void Start()
    {
        panel.gameObject.SetActive(false);
        
        if(PlayerPrefs.HasKey("TutorialCompleted") == false || PlayerPrefs.GetInt("TutorialCompleted") == 0)
        {
            StartCoroutine(StartShow());
        }
    }

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        print("Tutorial reset.");
        PlayerPrefs.SetInt("TutorialCompleted", 0);
    }

    IEnumerator StartShow()
    {
        yield return new WaitForSeconds(startDelay);

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
        PlayerPrefs.SetInt("TutorialCompleted", 1);

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
