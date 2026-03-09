using UnityEngine;

public class BackdoorManager : MonoBehaviour
{
    void OnEnable()
    {
        GlobalEventManager.OnBackdoorActive += ActivateBackdoor;
    }

    void OnDisable()
    {
        GlobalEventManager.OnBackdoorActive -= ActivateBackdoor;
    }

    void ActivateBackdoor()
    {
        Debug.Log("백도어 작동. 화이트해커의 시스템을 교란하여 치료 속도를 늦춥니다.");

        // 예: 치료 게이지를 20% 깎아버리거나, 치료 속도를 일시 정지
        WhiteHackerManager wh = FindObjectOfType<WhiteHackerManager>();
        if (wh != null)
        {
            wh.cureProgress -= 20f;
            if (wh.cureProgress < 0) wh.cureProgress = 0;
        }
    }
}