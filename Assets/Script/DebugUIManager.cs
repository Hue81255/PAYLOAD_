using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 필요

public class DebugUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI statText;
    public TextMeshProUGUI cureProgressText;
    public TextMeshProUGUI logText;

    void OnEnable()
    {
        // 이벤트 구독 (유튜브 구독 알림처럼!)
        GlobalEventManager.OnTimeChanged += UpdateTimeUI;
        GlobalEventManager.OnHackSuccess += UpdateCoinUI;
        GlobalEventManager.OnBackdoorActive += ShowBackdoorLog;
    }

    void OnDisable()
    {
        GlobalEventManager.OnTimeChanged -= UpdateTimeUI;
        GlobalEventManager.OnHackSuccess -= UpdateCoinUI;
        GlobalEventManager.OnBackdoorActive -= ShowBackdoorLog;
    }

    void UpdateTimeUI(float time, bool isNight)
    {
        string phase = isNight ? "밤 (침투 유리)" : "낮 (확산 유리)";
        timeText.text = $"시간: {time:F1}시 ({phase})";
    }

    void UpdateCoinUI(string id, int reward)
    {
        // EvolutionManager에 저장된 최신 코인 정보를 가져와 표시
        coinText.text = $"보유 코인: {EvolutionManager.Instance.currentCoin}";
        logText.text = $"최근 활동: {id} 해킹 성공! (+{reward})";
    }

    void Update()
    {
        // 매 프레임 변하는 스탯과 치료 게이지 표시
        statText.text = $"스탯 - 전염:{EvolutionManager.Instance.infectionLevel} | 은신:{EvolutionManager.Instance.stealthLevel} | 복잡:{EvolutionManager.Instance.complexityLevel}";

        var wh = FindObjectOfType<WhiteHackerManager>();
        if (wh != null)
        {
            cureProgressText.text = $"치료 진행도: {wh.cureProgress:F1}%";
            cureProgressText.color = wh.cureProgress > 80f ? Color.red : Color.white;
        }
    }

    void ShowBackdoorLog()
    {
        logText.text = "경고: 백도어 가동! 치료 차단 중!";
    }
}