using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public TextMeshProUGUI maintext;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maintext.text = "Hello";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Changetext()
    {
        maintext.text = "Change";
    }

    public void Changescene()
    {
        SceneManager.LoadScene("Museum");

    }
}
