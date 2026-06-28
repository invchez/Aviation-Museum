using System.Collections;
using UnityEngine;

public class AspectRatioController : MonoBehaviour {
    private IEnumerator MaintainAspectRatio()
    {
        while (true)
        {
            float desiredHeight = Screen.width / 16f * 9f;
            if (Screen.height != (int)desiredHeight)
            {
                Screen.SetResolution(Screen.width, (int)desiredHeight, false);
            }
            yield return new WaitForSeconds(0.5f); // Adjust as needed for performance vs smoothness
        }
    }

    void Start()
    {
        StartCoroutine(MaintainAspectRatio());
    }
}