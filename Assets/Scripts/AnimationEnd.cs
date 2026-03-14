using UnityEngine;

public class AnimationEnd : MonoBehaviour
{
    public delegate void onAnimationEnded();
    public event onAnimationEnded AnimationEndedEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AnimationEnded()
    {
        AnimationEndedEvent?.Invoke();
    }
}
