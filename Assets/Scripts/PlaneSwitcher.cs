using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlaneData
{
    public GameObject planeObject;
    public PlaneSO planeData;
}

[System.Serializable]
public class PlanePlot
{
    public TriggerScript panelTrigger;
    public List<PlaneData> planeData = new();
}

[System.Serializable]
public class LayoutButton
{
    public string layoutName;
    public Button layoutButton; 
}

public class PlaneSwitcher : MonoBehaviour
{
    public int currentLayout = 0;
    public List<PlanePlot> planePlot = new();
    public List<string> layoutButtonLabels = new();
    public Button buttonPrefab;

    [Title("UI")]
    public Toggle panelTriggerMeshToggle;

    public Transition transition;
    public float transitionDuration = 1f;

    Coroutine switchRoutine;
    List<Button> spawnedLayoutButtons = new();

    void Start()
    {
        BindPanelTriggerMeshToggle();

        for (int i = 0; i < layoutButtonLabels.Count; i++)
        {
            int layoutIndex = i; // Create a local copy of the loop variable
            Button newButton = Instantiate(buttonPrefab, buttonPrefab.transform.parent);
            newButton.gameObject.SetActive(true);

            spawnedLayoutButtons.Add(newButton);
            newButton.onClick.AddListener(() => SwitchLayout(layoutIndex));
            newButton.GetComponentInChildren<TMP_Text>().text = layoutButtonLabels[i];
        }

        SwitchLayout(currentLayout, doFade: false);

        buttonPrefab.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (panelTriggerMeshToggle == null)
        {
            return;
        }

        panelTriggerMeshToggle.onValueChanged.RemoveListener(SetPanelTriggerMeshRenderersVisible);
    }

    void BindPanelTriggerMeshToggle()
    {
        if (panelTriggerMeshToggle == null)
        {
            return;
        }

        panelTriggerMeshToggle.onValueChanged.RemoveListener(SetPanelTriggerMeshRenderersVisible);
        panelTriggerMeshToggle.onValueChanged.AddListener(SetPanelTriggerMeshRenderersVisible);

        SetPanelTriggerMeshRenderersVisible(panelTriggerMeshToggle.isOn);
    }

    public void SetPanelTriggerMeshRenderersVisible(bool isVisible)
    {
        for (int i = 0; i < planePlot.Count; i++)
        {
            TriggerScript trigger = planePlot[i].panelTrigger;
            if (trigger == null)
            {
                continue;
            }

            MeshRenderer meshRenderer = trigger.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                continue;
            }

            meshRenderer.enabled = isVisible;
        }
    }

    [Button("Switch To Layout")]
    void SwitchLayoutEditor(int layoutIndex)
    {
        SwitchLayout(layoutIndex, doFade: false);
    }

    public void SwitchLayout(int layout, bool doFade = true)
    {
        if (switchRoutine != null) StopCoroutine(switchRoutine);

        switchRoutine = StartCoroutine(SwitchLayoutWithTransition(layout, doFade));
    }

    IEnumerator SwitchLayoutWithTransition(int layout, bool doFade = true)
    {
        if (doFade)
        {
            transition.FadeIn();

            yield return new WaitForSeconds(transitionDuration);
        }

        foreach (var plot in planePlot)
        {
            // Fail-safe: if this plot has no planes configured, clear trigger data and skip safely.
            if (plot.planeData == null || plot.planeData.Count == 0)
            {
                if (plot.panelTrigger != null)
                {
                    plot.panelTrigger.Plane = null;
                    plot.panelTrigger.SetTriggerAllowed(false);
                    plot.panelTrigger.RefreshPanel();
                }

                continue;
            }

            bool hasRequestedLayout = layout >= 0 && layout < plot.planeData.Count;

            for (int i = 0; i < plot.planeData.Count; i++)
            {
                if (plot.planeData[i].planeObject != null)
                {
                    // If the requested layout is invalid for this plot, all planes are hidden.
                    plot.planeData[i].planeObject.SetActive(hasRequestedLayout && layout == i);
                }
            }

            if (plot.panelTrigger != null)
            {
                plot.panelTrigger.SetTriggerAllowed(hasRequestedLayout);
                plot.panelTrigger.Plane = hasRequestedLayout ? plot.planeData[layout].planeData : null;
                plot.panelTrigger.RefreshPanel();
            }
        }

        if (panelTriggerMeshToggle != null)
        {
            SetPanelTriggerMeshRenderersVisible(panelTriggerMeshToggle.isOn);
        }

        if (doFade)
        {
            transition.FadeOut();
        }

        yield return null;
    }

}
