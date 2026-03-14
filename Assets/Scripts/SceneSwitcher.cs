using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneSwitcher : MonoBehaviour
{

    public GameObject Panel;
    public List <Animator> ButtonAnimators;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Panel.GetComponent<AnimationEnd>().AnimationEndedEvent += DeactivatePanel;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")){
            Panel.SetActive(true);
            Panel.GetComponent<Animator>().Play("SceneSwitcherFadeIn");
            foreach (Animator animator in ButtonAnimators)
            {
                animator.Play("SceneSwitcherButtonFadeIn");
            }
        }
            
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")){
            Panel.GetComponent<Animator>().Play("SceneSwitcherFadeOut");
            foreach (Animator animator in ButtonAnimators)
            {
                animator.Play("SceneSwitcherButtonFadeOut");
            }

            
        }
            
    }

    public void SwitchScenes(string Scene)
    {
        GameObject.FindWithTag("transition").GetComponent<Transition>().FadeIn(Scene);
    }

    void DeactivatePanel()
    {
        Panel.SetActive(false);
    }
}
