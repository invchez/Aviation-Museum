using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Transition : MonoBehaviour
{
    string SceneName = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu")
        {
            GetComponent<Image>().color = Color.black;
            GetComponent<Animator>().Play("MainMenuFadeOut");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void FadeIn(string SelectedSceneSwitch)
    {
        SceneName = SelectedSceneSwitch;
        GetComponent<Animator>().Play("MainMenuFadeIn");
    }

    public void Changescene()
    {
        SceneManager.LoadScene(SceneName);

    }


}
