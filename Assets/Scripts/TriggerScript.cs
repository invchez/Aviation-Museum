using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TriggerScript : MonoBehaviour
{
    public GameObject StarterMain;
    public GameObject AiPanel;
    public GameObject StatisticsPanel;
    public GameObject InformationPanel;
    public PlaneSO Plane;
    public TextMeshProUGUI StatisticsText;
    public Canvas Canvas;
    private AudioSource TextToSpeech;
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        InformationPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Plane.Name;
        InformationPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Plane.Information;
        StatisticsText.text = $"WingSpan: {Plane.WingSpan}\n Top Speed: {Plane.TSpeed}";

        Canvas.worldCamera = Camera.main;
        GetComponentInChildren<AiTextReciever>(true).plane = Plane;

        TextToSpeech = GameObject.FindWithTag("AudioSource").GetComponent<AudioSource>();

        StarterMain.GetComponent<AnimationEnd>().AnimationEndedEvent += () => StarterMain.SetActive(false);
        AiPanel.GetComponent<AnimationEnd>().AnimationEndedEvent += () => AiPanel.SetActive(false);
        StatisticsPanel.GetComponent<AnimationEnd>().AnimationEndedEvent += () => StatisticsPanel.SetActive(false);
        InformationPanel.GetComponent<AnimationEnd>().AnimationEndedEvent += () => InformationPanel.SetActive(false);

        StarterMain.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Plane.Name;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) {
            AnimatePanels("StarterMain");
        }
            
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")){
            if (StarterMain.activeSelf)
            {
                StarterMain.GetComponent<Animator>().Play("PanelFadeOut");
            }

            else if (AiPanel.activeSelf)
            {
                AiPanel.GetComponent<Animator>().Play("AiPanelFadeOut");
            }

            else if (StatisticsPanel.activeSelf)
            {
                StatisticsPanel.GetComponent<Animator>().Play("StatPanelFadeOut");
            }
            
            else if (InformationPanel.activeSelf)
            {
                InformationPanel.GetComponent<Animator>().Play("InfoPanelFadeOut");
            }

            TextToSpeech.Stop();
        }
    }

    public void AnimatePanels(string PanelName)
    {
        if (PanelName == "StarterMain")
        {
            StarterMain.SetActive(true);
            StarterMain.GetComponent<Animator>().Play("PanelFadeIn");
        }

        else if (PanelName == "AIPanel")
        {
            AiPanel.SetActive(true);
            AiPanel.GetComponent<Animator>().Play("AiPanelFadeIn");
        }

        else if (PanelName == "InfoPanel")
        {
            InformationPanel.SetActive(true);
            InformationPanel.GetComponent<Animator>().Play("InfoPanelFadeIn");
        }

        else if (PanelName == "StatPanel")
        {
            StatisticsPanel.SetActive(true);
            StatisticsPanel.GetComponent<Animator>().Play("StatPanelFadeIn");
        }

    }
}
