using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TriggerScript : MonoBehaviour
{
    public bool allowTrigger = true;
    public GameObject StarterMain;
    public GameObject AiPanel;
    public GameObject StatisticsPanel;
    public GameObject InformationPanel;
    public PlaneSO Plane;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI StatisticsText;
    public Canvas Canvas;
    private AudioSource TextToSpeech;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        RefreshPanel();
    }

    // Centralized visibility cleanup so disabling a trigger always closes every panel immediately.
    void HideAllPanelsAndStopAudio()
    {
        if (StarterMain != null) StarterMain.SetActive(false);
        if (AiPanel != null) AiPanel.SetActive(false);
        if (StatisticsPanel != null) StatisticsPanel.SetActive(false);
        if (InformationPanel != null) InformationPanel.SetActive(false);

        if (TextToSpeech != null) TextToSpeech.Stop();
    }

    // Use this instead of writing allowTrigger directly when another system toggles trigger availability.
    public void SetTriggerAllowed(bool isAllowed)
    {
        allowTrigger = isAllowed;

        if (!allowTrigger)
        {
            // Important for runtime layout switching: player may already be inside the trigger.
            HideAllPanelsAndStopAudio();
        }
    }

    public void RefreshPanel()
    {
        // When no plane is assigned we treat this trigger as inactive and clear visible text.
        if (Plane == null)
        {
            HideAllPanelsAndStopAudio();

            if (InformationPanel != null)
            {
                if (InformationPanel.transform.childCount > 0)
                {
                    TextMeshProUGUI infoTitle = InformationPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (infoTitle != null) infoTitle.text = string.Empty;
                }

                if (InformationPanel.transform.childCount > 2)
                {
                    TextMeshProUGUI infoBody = InformationPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
                    if (infoBody != null) infoBody.text = string.Empty;
                }
            }

            if (StatisticsText != null) StatisticsText.text = string.Empty;
            if (TitleText != null) TitleText.text = string.Empty;

            return;
        }

        InformationPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Plane.Name;
        InformationPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Plane.Information;
        StatisticsText.text = $"WingSpan: {Plane.WingSpan}\n Top Speed: {Plane.TSpeed}";
        TitleText.text = $"{Plane.Name}";

        Canvas.worldCamera = Camera.main;
        GetComponentInChildren<AiTextReciever>(true).plane = Plane;

        TextToSpeech = GameObject.FindWithTag("AudioSource").GetComponent<AudioSource>();

        StarterMain.GetComponent<AnimationEnd>().AnimationEndedEvent += () => StarterMain.SetActive(false);
        AiPanel.GetComponent<AnimationEnd>().AnimationEndedEvent += () => AiPanel.SetActive(false);
        StatisticsPanel.GetComponent<AnimationEnd>().AnimationEndedEvent += () => StatisticsPanel.SetActive(false);
        InformationPanel.GetComponent<AnimationEnd>().AnimationEndedEvent += () => InformationPanel.SetActive(false);

        StarterMain.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Plane.Name;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!allowTrigger)
        {
            HideAllPanelsAndStopAudio();
            return;
        }

        if (other.CompareTag("Player")) {
            AnimatePanels("StarterMain");
        }
            
    }

    private void OnTriggerExit(Collider other)
    {
        if (!allowTrigger)
        {
            HideAllPanelsAndStopAudio();
            return;
        }

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

            if (TextToSpeech != null) TextToSpeech.Stop();
        }
    }

    public void AnimatePanels(string PanelName)
    {
        if (!allowTrigger)
        {
            HideAllPanelsAndStopAudio();
            return;
        }

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
