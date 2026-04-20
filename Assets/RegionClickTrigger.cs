using UnityEngine;

public class RegionClickTrigger : MonoBehaviour
{
    public Animator cameraAnimator;
    public string triggerName;

    private void OnMouseDown()
    {
        Debug.Log("Clicked: " + triggerName);
        cameraAnimator.SetTrigger(triggerName);
    }
}