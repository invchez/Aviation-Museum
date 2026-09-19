using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fullscreen fade overlay. Scene loading is owned by SceneSwitcher, which starts the
// fade and then loads the scene itself; this component only drives the animation.
public class Transition : MonoBehaviour
{
    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu")
        {
            GetComponent<Image>().color = Color.black;
            GetComponent<Animator>().Play("MainMenuFadeOut");
        }
    }

    public void FadeOut()
    {
        GetComponent<Animator>().Play("MainMenuFadeOut");
    }

    public void FadeIn()
    {
        GetComponent<Animator>().Play("MainMenuFadeIn");
    }
}
