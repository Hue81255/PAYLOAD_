using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전염병 주식회사 방식의 자동 전파 시스템.
<<<<<<< Updated upstream
/// 감염된 구역에서 인접 미감염 구역으로 spreadInterval마다 전파를 시도한다.
=======
/// 감염된 지역에서 인접 미감염 지역으로 spreadInterval마다 전파를 시도한다.
/// ConfirmStartInfection() 이후 GameManager가 StartSpread()를 호출해 활성화된다.
>>>>>>> Stashed changes
/// </summary>
public class SpreadManager : MonoBehaviour
{
    public static SpreadManager Instance;

    [Header("전파 설정")]
    [Tooltip("전파 시도 주기 (초)")]
<<<<<<< Updated upstream
    public float spreadInterval = 10f;
=======
    public float spreadInterval = 15f;
>>>>>>> Stashed changes

    [Tooltip("기본 전파 확률 (0~1)")]
    [Range(0f, 1f)]
    public float baseSpreadChance = 0.35f;

    [Tooltip("낮 시간대 전파 속도 배율")]
    public float daySpreadMultiplier   = 1.0f;
    [Tooltip("밤 시간대 전파 속도 배율 (더 빠르게)")]
    public float nightSpreadMultiplier = 1.4f;

<<<<<<< Updated upstream
    private bool _isRunning = false;
    private float _spreadTimer = 0f;

    // UIManager에서 다음 전파까지 남은 시간 표시용
    public float NextSpreadIn => _isRunning ? Mathf.Max(0f, spreadInterval - _spreadTimer) : 0f;
    public float SpreadProgress => _isRunning ? Mathf.Clamp01(_spreadTimer / spreadInterval) : 0f;
=======
    // ── 생명주기 ──────────────────────────────────────────────────
>>>>>>> Stashed changes

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── 공개 API ──────────────────────────────────────────────────

    public void StartSpread()
    {
<<<<<<< Updated upstream
        if (_isRunning) return;
        _isRunning = true;
        _spreadTimer = 0f;
=======
        StopAllCoroutines();
>>>>>>> Stashed changes
        StartCoroutine(SpreadCoroutine());
        Debug.Log("[SpreadManager] 바이러스 자동 전파 시작.");
    }

    public void StopSpread()
    {
<<<<<<< Updated upstream
        _isRunning = false;
        _spreadTimer = 0f;
=======
>>>>>>> Stashed changes
        StopAllCoroutines();
        Debug.Log("[SpreadManager] 바이러스 전파 정지.");
    }

    // ── 전파 코루틴 ───────────────────────────────────────────────

    IEnumerator SpreadCoroutine()
    {
<<<<<<< Updated upstream
        while (_isRunning)
        {
            yield return null; // 매 프레임 타이머 갱신 (NextSpreadIn 정확도)
            if (!GameManager.Instance.isGameStarted) { yield return null; continue; }

            _spreadTimer += Time.deltaTime;

            if (_spreadTimer >= spreadInterval)
            {
                _spreadTimer = 0f;
                TrySpreadAll();
            }
=======
        while (true)
        {
            yield return new WaitForSeconds(spreadInterval);

            if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) continue;

            TrySpreadAll();
>>>>>>> Stashed changes
        }
    }

    void TrySpreadAll()
    {
        if (InfectionEngine.Instance == null || RegionAdjacencyManager.Instance == null) return;

<<<<<<< Updated upstream
=======
        // 현재 감염 지역 목록을 복사 (반복 중 리스트 변경 방지)
>>>>>>> Stashed changes
        var infected = new List<RegionData>(
            InfectionEngine.Instance.regions.FindAll(r => r.isInfected));

        float timeMultiplier = GetTimeMultiplier();
<<<<<<< Updated upstream
        int successCount = 0;
=======
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
                    successCount++;
                }
            }
        }

        if (successCount == 0 && infected.Count > 0)
        {
            // 전파 실패 시 조용히 진행 (전파 실패를 매번 알리면 노이즈)
            Debug.Log("[SpreadManager] 이번 전파 주기: 전파 없음");
        }
=======
                }
            }
        }
>>>>>>> Stashed changes
    }

    // ── 전파 확률 계산 ────────────────────────────────────────────

    float CalcSpreadChance(RegionData target)
    {
        int playerInf    = InfectionEngine.Instance != null ? InfectionEngine.Instance.playerInf : 10;
        int adjReduction = target.defenseReduction;
        int effectiveDef = Mathf.Max(1, target.minStats.inf - adjReduction);

<<<<<<< Updated upstream
        // 플레이어 전염도가 방어력을 크게 초과할수록 확률 증가
        float chance = baseSpreadChance + (playerInf - effectiveDef) * 0.015f;
        return Mathf.Clamp(chance, 0.05f, 0.95f);
=======
        // 플레이어 전염도가 방어력보다 높을수록 확률 증가
        float chance = baseSpreadChance + (playerInf - effectiveDef) * 0.01f;
        return Mathf.Clamp(chance, 0.05f, 0.90f);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        UIManager.Instance?.ShowWarning($"[전파] {region.name} 지역에 바이러스가 침투했습니다!");
=======
        UIManager.Instance?.ShowWarning($"[전파] {region.name} 지역에 바이러스가 퍼졌습니다!");
        Debug.Log($"[SpreadManager] {region.name} 자동 전파 감염 성공.");
>>>>>>> Stashed changes
    }
}
