using UnityEngine;
using System.Collections.Generic;

public class InfectionEngine : MonoBehaviour
{
    public static InfectionEngine Instance;

    public int playerInf = 10;
    public int playerComp = 10;
    public int playerStealth = 10;

    public HackEffect hackEffect;

    // ������ ���� ���
    public List<RegionData> regions = new List<RegionData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AttemptHack(RegionData target)
    {
        if (target == null || target.isInfected) return;

        // Polymorphic 패시브: 화이트해커에 치료된 구역이면 요구 스탯 -20
        int reduction = 0;
        if (MalwareSelectionManager.Instance != null &&
            MalwareSelectionManager.Instance.IsPolymorphicCuredRegion(target.name))
            reduction = 20;

        // 인접 구역 감염에 의한 방어력 약화 합산
        int totalReduction = reduction + target.defenseReduction;
        int minStat = RegionAdjacencyManager.Instance != null
            ? RegionAdjacencyManager.Instance.minEffectiveStat : 5;

        // 해당 지역에 해제된 트레이트 노드 보너스 합산
        int bInf     = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Inf)     ?? 0;
        int bComp    = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Comp)    ?? 0;
        int bStealth = TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(target.id, TraitTree.TraitCategory.Stealth) ?? 0;

        bool success = (playerInf     + bInf)     >= Mathf.Max(minStat, target.minStats.inf     - totalReduction) &&
                       (playerComp    + bComp)    >= Mathf.Max(minStat, target.minStats.comp    - totalReduction) &&
                       (playerStealth + bStealth) >= Mathf.Max(minStat, target.minStats.stealth - totalReduction);

        if (success)
        {
            target.isInfected = true;
            GameManager.Instance?.OnRegionInfected();
            GlobalEventManager.CallHackSuccess(target.id, target.reward);
            if (hackEffect != null)
                hackEffect.PlaySuccess();
        }
        else
        {
            // Zero-Day 패시브: 실패 시 부족한 스탯 자동 +2
            if (MalwareSelectionManager.Instance != null &&
                MalwareSelectionManager.Instance.HasZeroDayPassive &&
                PlayerStats.Instance != null)
            {
                if (playerInf < target.minStats.inf) PlayerStats.Instance.UpgradeInf(2);
                if (playerComp < target.minStats.comp) PlayerStats.Instance.UpgradeComp(2);
                if (playerStealth < target.minStats.stealth) PlayerStats.Instance.UpgradeStealth(2);
                UIManager.Instance?.ShowWarning($"[제로데이] 취약점 학습: 스탯 자동 강화 +2");
            }
            else
            {
                UIManager.Instance?.ShowWarning($"{target.name} — 방어가 너무 강합니다!");
            }
            if (hackEffect != null)
                hackEffect.PlayFail();
        }
    }

    // ������ ���� �� �������� �ϳ� ��ȯ
    public string GetRandomInfectedRegion()
    {
        List<string> infected = new List<string>();
        foreach (var region in regions)
        {
            if (region.isInfected)
                infected.Add(region.name);
        }
        if (infected.Count == 0) return "";
        return infected[Random.Range(0, infected.Count)];
    }

    // 구역 치료 (화이트해커 전용)
    public void CureRegion(string regionName)
    {
        var region = regions.Find(r => r.name == regionName);
        if (region == null) return;

        region.isInfected = false;
        region.defenseReduction = 0;

        // 인접 구역 방어력 재계산
        RegionAdjacencyManager.Instance?.OnRegionCured(region.id);

        Debug.Log($"[InfectionEngine] {regionName} 치료 완료");
    }
}
