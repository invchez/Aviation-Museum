using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneSwitcher : MonoBehaviour
{
    public Animator panel;

    void Start()
    {
        panel.gameObject.SetActive(false);
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
}
