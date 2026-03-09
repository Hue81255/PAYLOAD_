using UnityEngine;

public class RegionController : MonoBehaviour
{
    public string regionId;
    public RegionData data;

    // 실시간으로 변동되는 현재 스탯
    public float currentInf;
    public float currentStealth;

    void OnEnable()
    {
        GlobalEventManager.OnTimeChanged += UpdateStatsByTime;
    }

    void OnDisable()
    {
        GlobalEventManager.OnTimeChanged -= UpdateStatsByTime;
    }

    void UpdateStatsByTime(float time, bool isNight)
    {
        if (data == null) return;

        // 기획안 반영: 중구 같은 업무지구는 밤에 모바일 기기(전염 발판)가 감소하여 난이도 상승
        if (data.type == "Business")
        {
            if (isNight)
            {
                // 밤에는 기기가 적어 침투가 더 어려워짐 (최소 요구 스탯 가중치 증가)
                currentInf = data.minStats.inf * 1.5f;
                currentStealth = data.minStats.stealth * 0.8f; // 대신 사람이 없어서 은신은 조금 쉬워짐
            }
            else
            {
                currentInf = data.minStats.inf;
                currentStealth = data.minStats.stealth;
            }
        }
        // 주거 지역은 밤에 IOT 기기 활성으로 전염이 더 쉬워질 수 있음
        else if (data.type == "Residential")
        {
            currentInf = isNight ? data.minStats.inf * 0.7f : data.minStats.inf;
        }
    }
}