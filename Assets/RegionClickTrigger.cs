using UnityEngine;

public class RegionClickTrigger : MonoBehaviour
{
    public Animator cameraAnimator;
    public string triggerName;

    private void OnMouseDown()
    {
        Debug.Log("Clicked: " + triggerName);

        if (cameraAnimator == null)
        {
            Debug.LogWarning($"{triggerName}: cameraAnimator 연결 안됨!");
            return;
        }

        cameraAnimator.SetTrigger(triggerName);
    }
}
