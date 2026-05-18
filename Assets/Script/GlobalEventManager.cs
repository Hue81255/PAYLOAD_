using System;
using UnityEngine;

public static class GlobalEventManager
{
    // 해킹 성공 이벤트: (구역 ID, 획득한 코인 보상)
    public static Action<string, int> OnHackSuccess;

    // 시간 변경 이벤트: (현재 시각 0~24, 밤 여부 true/false)
    public static Action<float, bool> OnTimeChanged;

    // 백도어 활성화 이벤트
    public static Action OnBackdoorActive;

    // 구역 방어력 변동 이벤트: (구역 ID, 새 감소량)
    public static Action<string, int> OnDefenseChanged;

    // 이벤트 발동(Trigger) 메서드
    public static void CallHackSuccess(string id, int reward)       => OnHackSuccess?.Invoke(id, reward);
    public static void CallTimeChanged(float time, bool isNight)    => OnTimeChanged?.Invoke(time, isNight);
    public static void CallBackdoorActive()                         => OnBackdoorActive?.Invoke();
    public static void CallDefenseChanged(string id, int reduction) => OnDefenseChanged?.Invoke(id, reduction);
}
