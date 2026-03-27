using System.Collections;
using System.Collections.Generic;
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

public class PlaneSwitcher : MonoBehaviour
{
    public int currentLayout = 0;
    public List<PlanePlot> planePlot = new();
    public List<Button> layoutSwitchButtons = new();

    public Transition transition;
    public float transitionDuration = 1f;

    Coroutine switchRoutine;

    void Start()
    {
        for (int i = 0; i < layoutSwitchButtons.Count; i++)
        {
            int layoutIndex = i; // Create a local copy of the loop variable
            layoutSwitchButtons[i].onClick.AddListener(() => SwitchLayout(layoutIndex));
        }

        SwitchLayout(currentLayout, doFade: false);
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
            for (int i = 0; i < plot.planeData.Count; i++)
            {
                plot.planeData[i].planeObject.SetActive(layout == i);
                plot.panelTrigger.Plane = plot.planeData[layout].planeData;
                plot.panelTrigger.RefreshPanel();
            }
        }

        if (doFade)
        {
            transition.FadeOut();
        }

        yield return null;
    }

}
