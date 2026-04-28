using System;
using UnityEngine;

public static class GlobalEventManager
{
    // 해킹 성공 이벤트: (구역 ID, 획득한 보상 코인)
    public static Action<string, int> OnHackSuccess;

    // 시간 변경 이벤트: (현재 시간 0~24, 밤 여부 true/false)
    public static Action<float, bool> OnTimeChanged;

    // 백도어 활성화 이벤트
    public static Action OnBackdoorActive;

    // 이벤트 실행(Trigger) 메소드들
    public static void CallHackSuccess(string id, int reward) => OnHackSuccess?.Invoke(id, reward);
    public static void CallTimeChanged(float time, bool isNight) => OnTimeChanged?.Invoke(time, isNight);
    public static void CallBackdoorActive() => OnBackdoorActive?.Invoke();
}