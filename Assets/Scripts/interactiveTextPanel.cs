using UnityEngine;
using TMPro;


public class interactiveTextPanel : MonoBehaviour
{
    public TMP_InputField inputfield;
    public GameObject StarterMainPanel;

    // Where the question field is lifted to while typing, as a fraction of screen height
    // from the top. The landscape keyboard begins about 48% down on iPad and 43% on
    // iPhone, so this keeps the field and the latest answer lines visible above it.
    const float FieldFromTopWhileTyping = 0.28f;

    PlayerScript playerScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var playerobject = GameObject.Find("Player");
        playerScript = playerobject.GetComponent<PlayerScript>();

        inputfield.onEndEdit.AddListener((string str) => {
            playerScript.SetCanMove(true);
            LowerFromKeyboard();
        });

        inputfield.onSelect.AddListener((string str) => {
            playerScript.SetCanMove(false);
            RaiseAboveKeyboard();
        });
    }

    public void TogglePanel()
    {
        StarterMainPanel.SetActive(false);
    }

    // The exhibit panel is in the world, so the on-screen keyboard covers the question
    // field unless the view tilts to lift the panel above it. TouchScreenKeyboard.area
    // reports nothing on iPad here, so the lift targets a fixed screen position instead
    // of measuring the keyboard.
    void RaiseAboveKeyboard()
    {
        Camera view = Camera.main;
        if (!playerScript.IsMobileModeEnabled() || view == null)
        {
            return;
        }

        float aboveCentre = 0.5f - FieldFromTopWhileTyping;
        float halfFov = view.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float tilt = Mathf.Atan(2f * aboveCentre * Mathf.Tan(halfFov)) * Mathf.Rad2Deg;

        playerScript.FaceTowards(inputfield.transform.position, tilt, 0.35f);
    }

    void LowerFromKeyboard()
    {
        if (!playerScript.IsMobileModeEnabled())
        {
            return;
        }

        Canvas panel = inputfield.GetComponentInParent<Canvas>();
        if (panel != null)
        {
            playerScript.FaceTowards(panel.transform.position, 0f, 0.35f);
        }
    }
}
