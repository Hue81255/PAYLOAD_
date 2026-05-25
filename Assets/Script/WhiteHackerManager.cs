using UnityEngine;
using System.Collections.Generic;

public class WhiteHackerManager : MonoBehaviour
{
    public static WhiteHackerManager Instance;

    public enum HackerState { Idle, Scanning, Curing, Alert }

    [Header("AI 상태")]
    public HackerState currentState = HackerState.Idle;

    [Header("AI 설정")]
    public float scanInterval = 10f;
    public float cureTime = 15f;
    public int   coinPenalty = 50;

    [Header("상태별 치료 속도 배율")]
    public float idleMultiplier  = 1f;
    public float alertMultiplier = 2f;

    private float  scanTimer       = 0f;
    private string targetRegion    = "";
    private float  regionCureTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;

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

    void UpdateState()
    {
        float infectionRate   = GameManager.Instance != null
            ? (float)GameManager.Instance.infectedRegions / GameManager.Instance.totalRegions : 0f;
        float defenseProgress = CureManager.Instance != null ? CureManager.Instance.cureProgress : 0f;

        // 감염률 60% 이상 → 스캐닝 시작, 방어 진행도 60% 이상 → 경보(Alert, 속도 2배)
        if (!string.IsNullOrEmpty(targetRegion))
            currentState = HackerState.Curing;
        else if (defenseProgress >= 60f)
            currentState = HackerState.Alert;
        else if (infectionRate >= 0.60f)
            currentState = HackerState.Scanning;
        else
            currentState = HackerState.Idle;
    }

    void HandleScanning()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval && string.IsNullOrEmpty(targetRegion))
        {
            scanTimer = 0f;
            FindAndCureRegion();
        }
    }

    void HandleRegionCuring()
    {
        if (string.IsNullOrEmpty(targetRegion)) return;

        float multiplier = (currentState == HackerState.Alert) ? alertMultiplier : idleMultiplier;
        regionCureTimer += Time.deltaTime * multiplier;

        float effectiveCureTime = cureTime * (MalwareSelectionManager.Instance?.WhiteHackerCureTimeMultiplier ?? 1f);
        if (regionCureTimer >= effectiveCureTime)
            CompleteCure();
    }

    void FindAndCureRegion()
    {
        if (InfectionEngine.Instance == null) return;
        string infected = InfectionEngine.Instance.GetRandomInfectedRegion();
        if (!string.IsNullOrEmpty(infected))
        {
            targetRegion    = infected;
            regionCureTimer = 0f;
            UIManager.Instance?.ShowWarning($"화이트해커가 [{targetRegion}] 구역을 복구 중입니다!");
        }
    }

    void CompleteCure()
    {

        MalwareSelectionManager.Instance?.RegisterPolymorphicCuredRegion(targetRegion);

        if (InfectionEngine.Instance != null)
            InfectionEngine.Instance.CureRegion(targetRegion);

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddCoins(-coinPenalty);

        if (GameManager.Instance != null)
            GameManager.Instance.OnRegionCured();

        UIManager.Instance?.ShowWarning($"[{targetRegion}] 구역이 복구되었습니다! 코인 -{coinPenalty}");

        targetRegion    = "";
        regionCureTimer = 0f;
    }

    // ── 공개 유틸 ─────────────────────────────────────────────────

    public float GetRegionCureProgress()
    {
        if (string.IsNullOrEmpty(targetRegion)) return 0f;
        float effectiveCureTime = cureTime * (MalwareSelectionManager.Instance?.WhiteHackerCureTimeMultiplier ?? 1f);
        return Mathf.Clamp01(regionCureTimer / effectiveCureTime);
    }

    public string GetTargetRegion() => targetRegion;

    public void ResetScanTimer()        { scanTimer = 0f; }
    public void HalveRegionCureTimer()  { regionCureTimer /= 2f; }

    public void ResetAI()
    {
        currentState    = HackerState.Idle;
        regionCureTimer = 0f;
        scanTimer       = 0f;
        targetRegion    = "";
    }

    // ── 세이브/로드 ───────────────────────────────────────────────

    public void FillSaveData(SaveData data)
    {
        data.hackerState      = (int)currentState;
        data.isCuring         = !string.IsNullOrEmpty(targetRegion);
        data.targetRegion     = targetRegion;
        data.regionCureTimer  = regionCureTimer;
        data.scanTimer        = scanTimer;
        data.hackerCureProgress = 0f; // CureManager가 관리
    }

    public void ApplyLoadData(SaveData data)
    {
        currentState    = (HackerState)data.hackerState;
        targetRegion    = data.targetRegion;
        regionCureTimer = data.regionCureTimer;
        scanTimer       = data.scanTimer;
    }
}
