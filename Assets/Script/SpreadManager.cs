using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전염병 주식회사 방식의 자동 전파 시스템.
/// 감염된 구역에서 인접 미감염 구역으로 spreadInterval마다 전파를 시도한다.
/// </summary>
public class SpreadManager : MonoBehaviour
{
    public static SpreadManager Instance;

    [Header("전파 설정")]
    [Tooltip("전파 시도 주기 (초)")]
    public float spreadInterval = 10f;

    [Tooltip("기본 전파 확률 (0~1)")]
    [Range(0f, 1f)]
    public float baseSpreadChance = 0.35f;

    [Tooltip("낮 시간대 전파 속도 배율")]
    public float daySpreadMultiplier   = 1.0f;
    [Tooltip("밤 시간대 전파 속도 배율 (더 빠르게)")]
    public float nightSpreadMultiplier = 1.4f;

    private bool  _isRunning    = false;
    private float _spreadTimer  = 0f;

    // UIManager에서 다음 전파까지 남은 시간 표시용
    public float NextSpreadIn  => _isRunning ? Mathf.Max(0f, spreadInterval - _spreadTimer) : 0f;
    public float SpreadProgress => _isRunning ? Mathf.Clamp01(_spreadTimer / spreadInterval) : 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 공개 API ──────────────────────────────────────────────────

    public void StartSpread()
    {
        if (_isRunning) return;
        _isRunning   = true;
        _spreadTimer = 0f;
        StartCoroutine(SpreadCoroutine());
        Debug.Log("[SpreadManager] 바이러스 자동 전파 시작.");
    }

    public void StopSpread()
    {
        _isRunning   = false;
        _spreadTimer = 0f;
        StopAllCoroutines();
        Debug.Log("[SpreadManager] 바이러스 전파 정지.");
    }

    // ── 전파 코루틴 ───────────────────────────────────────────────

    IEnumerator SpreadCoroutine()
    {
        while (_isRunning)
        {
            yield return null; // 매 프레임 타이머 갱신 (NextSpreadIn 정확도)
            if (GameManager.Instance == null || !GameManager.Instance.isGameStarted)
            {
                yield return null;
                continue;
            }

            _spreadTimer += Time.deltaTime;

            if (_spreadTimer >= spreadInterval)
            {
                _spreadTimer = 0f;
                TrySpreadAll();
            }
        }
    }

    void TrySpreadAll()
    {
        if (InfectionEngine.Instance == null || RegionAdjacencyManager.Instance == null) return;

        var infected = new List<RegionData>(
            InfectionEngine.Instance.regions.FindAll(r => r.isInfected));

        float timeMultiplier = GetTimeMultiplier();
        int   successCount   = 0;

        foreach (var source in infected)
        {
            var adjacentIds = RegionAdjacencyManager.Instance.GetAdjacentIds(source.id);
            foreach (string adjId in adjacentIds)
            {
                var target = InfectionEngine.Instance.regions.Find(r => r.id == adjId);
                if (target == null || target.isInfected) continue;

                float chance = CalcSpreadChance(target) * timeMultiplier;
                if (Random.value < chance)
                {
                    InfectRegion(target);
                    successCount++;
                }
            }
        }

        if (successCount == 0 && infected.Count > 0)
            Debug.Log("[SpreadManager] 이번 전파 주기: 전파 없음");
    }

    // ── 전파 확률 계산 ────────────────────────────────────────────

    float CalcSpreadChance(RegionData target)
    {
        int playerInf    = InfectionEngine.Instance != null ? InfectionEngine.Instance.playerInf : 10;
        int adjReduction = target.defenseReduction;
        int effectiveDef = Mathf.Max(1, target.minStats.inf - adjReduction);

        float chance = baseSpreadChance + (playerInf - effectiveDef) * 0.015f;
        return Mathf.Clamp(chance, 0.05f, 0.95f);
    }

    float GetTimeMultiplier()
    {
        if (TimeManager.instance == null) return 1f;
        return TimeManager.instance.isNight ? nightSpreadMultiplier : daySpreadMultiplier;
    }

    // ── 실제 감염 처리 ────────────────────────────────────────────

    void InfectRegion(RegionData region)
    {
        region.isInfected = true;
        GameManager.Instance?.OnRegionInfected();
        GlobalEventManager.CallHackSuccess(region.id, region.reward);
        UIManager.Instance?.ShowWarning($"[전파] {region.name} 지역에 바이러스가 침투했습니다!");
    }
}
