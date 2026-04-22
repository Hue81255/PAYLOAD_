using UnityEngine;
using System.Collections.Generic;

public class WhiteHackerManager : MonoBehaviour
{
    public static WhiteHackerManager Instance;

    // AI 상태 머신 (FSM)
    public enum HackerState
    {
        Idle,       // 대기 중
        Scanning,   // 감염 구역 탐색 중
        Curing,     // 구역 치료 중
        Alert       // 경계 상태 (발각도 60% 이상)
    }

    [Header("현재 AI 상태")]
    public HackerState currentState = HackerState.Idle;

    [Header("치료 진행도")]
    public float cureProgress = 0f;
    public float cureSpeed = 0.5f;
    public bool isCuring = false;

    [Header("AI 설정")]
    public float scanInterval = 10f;   // 몇 초마다 감염 구역 탐색
    public float cureTime = 15f;       // 구역 하나 치료하는 데 걸리는 시간
    public int coinPenalty = 50;       // 치료 완료 시 코인 페널티

    [Header("상태별 치료 속도 배율")]
    public float idleMultiplier = 1f;
    public float alertMultiplier = 2f;

    private float scanTimer = 0f;
    private string targetRegion = "";
    private float regionCureTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // 기존 로직: 감염도 임계점 넘으면 치료 시작
        if (EvolutionManager.Instance.infectionLevel > 2 || isCuring)
        {
            isCuring = true;
            ExecuteCure();
        }

        // FSM 상태 업데이트
        UpdateState();

        switch (currentState)
        {
            case HackerState.Idle:
            case HackerState.Scanning:
                HandleScanning();
                break;
            case HackerState.Curing:
                HandleRegionCuring();
                break;
            case HackerState.Alert:
                HandleScanning();
                HandleRegionCuring();
                break;
        }
    }

    // 기존 전체 치료 진행도 로직 유지
    void ExecuteCure()
    {
        float complexityModifier = 1.0f / (EvolutionManager.Instance.complexityLevel * 0.5f + 1);
        cureProgress += cureSpeed * complexityModifier * Time.deltaTime;

        // 발각도와 연동
        if (CureManager.Instance != null)
            CureManager.Instance.cureProgress = cureProgress;

        if (cureProgress >= 100f)
        {
            Debug.Log("화이트해커가 바이러스를 완전히 박멸! 게임 오버!");
            GameManager.Instance.GameOver();
        }

        if (cureProgress >= 50f)
        {
            GlobalEventManager.CallBackdoorActive();
        }
    }

    // FSM 상태 전환
    void UpdateState()
    {
        if (isCuring && !string.IsNullOrEmpty(targetRegion))
            currentState = HackerState.Curing;
        else if (cureProgress >= 60f)
            currentState = HackerState.Alert;
        else if (cureProgress >= 30f)
            currentState = HackerState.Scanning;
        else
            currentState = HackerState.Idle;
    }

    // 감염 구역 탐색
    void HandleScanning()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval && string.IsNullOrEmpty(targetRegion))
        {
            scanTimer = 0f;
            FindAndCureRegion();
        }
    }

    // 개별 구역 치료
    void HandleRegionCuring()
    {
        if (string.IsNullOrEmpty(targetRegion)) return;

        float multiplier = (currentState == HackerState.Alert)
            ? alertMultiplier
            : idleMultiplier;

        regionCureTimer += Time.deltaTime * multiplier;

        if (regionCureTimer >= cureTime)
        {
            CompleteCure();
        }
    }

    void FindAndCureRegion()
    {
        if (InfectionEngine.Instance == null) return;

        string infected = InfectionEngine.Instance.GetRandomInfectedRegion();
        if (!string.IsNullOrEmpty(infected))
        {
            targetRegion = infected;
            regionCureTimer = 0f;
            Debug.Log($"화이트해커: [{targetRegion}] 구역 치료 시작!");
        }
    }

    void CompleteCure()
    {
        Debug.Log($"화이트해커: [{targetRegion}] 구역 치료 완료!");

        // 구역 감염 해제
        if (InfectionEngine.Instance != null)
            InfectionEngine.Instance.CureRegion(targetRegion);

        // 코인 페널티
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddCoins(-coinPenalty);
            Debug.Log($"코인 -{coinPenalty} 페널티!");
        }

        // GameManager에 알림
        if (GameManager.Instance != null)
            GameManager.Instance.OnRegionCured();

        targetRegion = "";
        regionCureTimer = 0f;
    }

    // UI용
    public float GetRegionCureProgress()
    {
        if (string.IsNullOrEmpty(targetRegion)) return 0f;
        return regionCureTimer / cureTime;
    }

    public string GetTargetRegion() { return targetRegion; }

    public void ResetAI()
    {
        currentState = HackerState.Idle;
        isCuring = false;
        cureProgress = 0f;
        regionCureTimer = 0f;
        scanTimer = 0f;
        targetRegion = "";
    }
}
