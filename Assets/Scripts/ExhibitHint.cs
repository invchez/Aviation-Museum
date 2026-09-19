using UnityEngine;

// On-screen prompt telling first-time visitors how to open an exhibit. The exhibit
// panels only appear once the visitor is standing in front of an aircraft, so without
// this there is nothing on screen to suggest they exist.
public class ExhibitHint : MonoBehaviour
{
    const string PrefExhibitOpened = "ExhibitOpened";

    void OnEnable()
    {
        TriggerScript.ExhibitEntered += Retire;
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(PrefExhibitOpened, 0) == 1)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        TriggerScript.ExhibitEntered -= Retire;
    }

    void Retire()
    {
        PlayerPrefs.SetInt(PrefExhibitOpened, 1);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
