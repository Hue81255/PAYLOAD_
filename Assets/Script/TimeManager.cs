using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float currentTime = 0f; // (0~24 시간 표현)
    public float timeSpeed = 1f;
    private bool isNight = false;

    void Update()
    {
        currentTime += Time.deltaTime * timeSpeed;
        if (currentTime >= 24f) currentTime = 0f;

        // 밤 시간대 체크 (20시 ~ 06시)
        bool nightCheck = (currentTime > 20f || currentTime < 6f);

        if (isNight != nightCheck)
        {
            isNight = nightCheck;
            // 상태가 변할 때만 이벤트 알림 발송
            GlobalEventManager.CallTimeChanged(currentTime, isNight);
        }
    }
}