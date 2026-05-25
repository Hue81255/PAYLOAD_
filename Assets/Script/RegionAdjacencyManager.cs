using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 구역 인접 관계를 관리하고, 구역이 감염/치료될 때 이웃 구역의 방어력을 갱신한다.
/// 인접 감염 구역 1개당 reductionPerNeighbor만큼 방어 스탯이 감소하며 스택된다.
/// </summary>
public class RegionAdjacencyManager : MonoBehaviour
{
    public static RegionAdjacencyManager Instance;

    [Header("약화 설정")]
    [Tooltip("인접 감염 구역 1개당 방어 스탯 감소량")]
    public int reductionPerNeighbor = 15;
    [Tooltip("약화 후 방어 스탯 최솟값 (0 이하로 내려가지 않음)")]
    public int minEffectiveStat = 5;

    // ─────────────────────────────────────────────────────────────
    // 대구광역시 실제 지리 기반 인접 관계 (양방향)
    // ─────────────────────────────────────────────────────────────
    // Data.json의 ID와 정확히 일치해야 함
    private static readonly Dictionary<string, List<string>> Adjacency =
        new Dictionary<string, List<string>>
        {
            { "JUNG_GU",      new List<string> { "DONG_GU", "SEO_GU", "NAM_GU", "BUK_GU", "SUSEONG_GU" } },
            { "DONG_GU",      new List<string> { "JUNG_GU", "BUK_GU", "SUSEONG_GU" } },
            { "SEO_GU",       new List<string> { "JUNG_GU", "BUK_GU", "DALSEO_GU" } },
            { "NAM_GU",       new List<string> { "JUNG_GU", "SUSEONG_GU", "DALSEO_GU" } },
            { "BUK_GU",       new List<string> { "JUNG_GU", "DONG_GU", "SEO_GU", "DALSEONG_GUN", "GUNWI_GUN" } },
            { "SUSEONG_GU",   new List<string> { "JUNG_GU", "DONG_GU", "NAM_GU" } },
            { "DALSEO_GU",    new List<string> { "SEO_GU", "NAM_GU", "DALSEONG_GUN" } },
            { "DALSEONG_GUN", new List<string> { "BUK_GU", "DALSEO_GU" } },
            { "GUNWI_GUN",    new List<string> { "BUK_GU" } },
        };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()  => GlobalEventManager.OnHackSuccess += OnRegionInfected;
    void OnDisable() => GlobalEventManager.OnHackSuccess -= OnRegionInfected;

    // ─────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────

    void OnRegionInfected(string regionId, int _)
    {
        // 새로 감염된 구역의 이웃들 방어력 재계산
        RefreshNeighbors(regionId);
    }

    // 치료 완료 시 InfectionEngine.CureRegion()에서 호출
    public void OnRegionCured(string regionId)
    {
        // 치료된 구역 자신의 reduction도 다시 계산
        Recalculate(regionId);
        // 이웃들도 재계산 (이 구역이 더 이상 위협이 아니므로)
        RefreshNeighbors(regionId);
    }

    // 게임 시작/재시작 시 호출
    public void ResetAll()
    {
        if (InfectionEngine.Instance == null) return;
        foreach (var r in InfectionEngine.Instance.regions)
            r.defenseReduction = 0;
    }

    // 로드 후 감염 상태 기반으로 모든 구역 방어력 재계산
    public void RecalculateAll()
    {
        if (InfectionEngine.Instance == null) return;
        foreach (var r in InfectionEngine.Instance.regions)
            Recalculate(r.id);
    }

    // ─────────────────────────────────────────────────────────────
    // 핵심 로직
    // ─────────────────────────────────────────────────────────────

    void RefreshNeighbors(string regionId)
    {
        if (!Adjacency.TryGetValue(regionId, out var neighbors)) return;
        foreach (string neighborId in neighbors)
            Recalculate(neighborId);
    }

    void Recalculate(string regionId)
    {
        RegionData region = Find(regionId);
        if (region == null) return;

        int infected = 0;
        if (Adjacency.TryGetValue(regionId, out var neighbors))
        {
            foreach (string nId in neighbors)
            {
                RegionData n = Find(nId);
                if (n != null && n.isInfected) infected++;
            }
        }

        int prev = region.defenseReduction;
        region.defenseReduction = infected * reductionPerNeighbor;

        if (prev != region.defenseReduction)
            GlobalEventManager.CallDefenseChanged(regionId, region.defenseReduction);
    }

    // ─────────────────────────────────────────────────────────────
    // 공개 유틸리티
    // ─────────────────────────────────────────────────────────────

    /// <summary>실제 적용되는 방어 스탯 반환 (UI 표시용)</summary>
    public DefenseStats GetEffectiveStats(RegionData target)
    {
        return new DefenseStats
        {
            inf     = Mathf.Max(minEffectiveStat, target.minStats.inf     - target.defenseReduction),
            comp    = Mathf.Max(minEffectiveStat, target.minStats.comp    - target.defenseReduction),
            stealth = Mathf.Max(minEffectiveStat, target.minStats.stealth - target.defenseReduction),
        };
    }

    /// <summary>특정 구역의 인접 구역 ID 목록 반환</summary>
    public List<string> GetAdjacentIds(string regionId) =>
        Adjacency.TryGetValue(regionId, out var list) ? list : new List<string>();

    RegionData Find(string id) =>
        InfectionEngine.Instance?.regions.Find(r => r.id == id);
}
