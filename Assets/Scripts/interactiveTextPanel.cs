using UnityEngine;
using TMPro;


public class interactiveTextPanel : MonoBehaviour
{
    public TMP_InputField inputfield;
    public GameObject StarterMainPanel;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var playerobject = GameObject.Find("Player");
        var playerscript = playerobject.GetComponent<PlayerScript>();
        

        inputfield.onEndEdit.AddListener((string str) => {playerscript.SetCanMove(true);});

        inputfield.onSelect.AddListener((string str) => {playerscript.SetCanMove(false);});

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TogglePanel()
    {
        StarterMainPanel.SetActive(false);
    }

}

