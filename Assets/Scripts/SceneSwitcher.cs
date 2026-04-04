using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneSwitcher : MonoBehaviour
{
    public bool turnOffAtStart = true;
    public Animator panel;

    void Start()
    {
        panel.gameObject.SetActive(!turnOffAtStart);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            panel.gameObject.SetActive(true);
            panel.GetComponent<Animator>().Play("SceneSwitcherFadeIn");
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            panel.GetComponent<Animator>().Play("SceneSwitcherFadeOut");
        }

    }

    public void SwitchScene(int sceneIndex)
    {
        StartCoroutine(PlayFade(sceneIndex));
    }

    IEnumerator PlayFade(int sceneIndex)
    {
        panel.GetComponent<Animator>().Play("MainMenuFadeIn");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(sceneIndex);
    }
}
