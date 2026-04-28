// =============================================
// EvolutionManager.cs  (v2)
// 4단계 변이 + 4종 업그레이드 트리 + 조건 해금 + 랜덤 이벤트 변이
// =============================================

using UnityEngine;
using System.Collections.Generic;

public class EvolutionManager : MonoBehaviour
{
    public PlayerStats    stats;
    public InfectionEngine infEngine;

    // ============================================================
    //  [1] 변이 단계 업그레이드 (1→2→3→4)
    //
    //  감염 설계 "변이 단계별 능력 차이" 직접 반영:
    //  1단계: 소폭 증가, 리스크 없음
    //  2단계: 특정 능력 집중 강화, 트레이드오프 시작
    //  3단계: 강력한 능력 + 명확한 약점
    //  4단계: 하이리스크/하이리턴
    // ============================================================

    // 단계별 코인 비용
    private static readonly int[] MUTATION_COST = { 0, 80, 200, 400 };
    // index: 0=1→2, 1=2→3, 2=3→4

    public bool TryMutate()
    {
        int current = stats.mutationStage;
        if (current >= PlayerStats.MAX_MUTATION)
        {
            Debug.Log("[Evolution] 이미 최종 변이 단계입니다.");
            return false;
        }

        // 조건 해금 체크 (감염 설계 "조건 해금 방식")
        if (!CheckMutationCondition(current + 1))
            return false;

        int cost = MUTATION_COST[current - 1];
        if (stats.coins < cost)
        {
            Debug.Log($"[Evolution] 코인 부족. 필요:{cost} 보유:{stats.coins}");
            return false;
        }

        stats.coins -= cost;
        ApplyMutationBonus(current + 1);
        stats.mutationStage++;

        stats.RecalculateDerived();
        infEngine?.OnStatsChanged(stats);

        Debug.Log($"[Evolution] {current}단계 → {current+1}단계 변이 완료! {stats}");
        return true;
    }

    // 변이 조건 해금 체크
    private bool CheckMutationCondition(int targetStage)
    {
        switch (targetStage)
        {
            case 2: return true; // 1→2: 조건 없음
            case 3:
                // 기관 1개 이상 점령 OR 감염률 50% 달성
                if (stats.capturedInstitutions < 1 && stats.maxInfectionRate < 50f)
                {
                    Debug.Log("[Evolution] 3단계 조건 미달: 기관 1개 점령 또는 감염률 50% 필요");
                    return false;
                }
                return true;
            case 4:
                // 탐지 회피 유지 시간 조건 + 기관 3개 이상
                if (stats.capturedInstitutions < 3)
                {
                    Debug.Log("[Evolution] 4단계 조건 미달: 기관 3개 이상 점령 필요");
                    return false;
                }
                return true;
            default: return true;
        }
    }

    // 변이 단계별 스탯 보너스 적용
    private void ApplyMutationBonus(int newStage)
    {
        switch (newStage)
        {
            case 2:
                // 2단계: 감염속도 +10~20%, 탐지확률 소폭 증가
                stats.infectionSpeed     = Mathf.Min(stats.infectionSpeed + 15f, 100f);
                stats.spreadRange        = Mathf.Min(stats.spreadRange    + 10f, 100f);
                // 탐지 위험 소폭 (RecalculateDerived에서 자동 반영)
                Debug.Log("  2단계: 감염속도+15, 확산범위+10");
                break;

            case 3:
                // 3단계: 트리 선택에 따라 집중 강화 (트리 없으면 기본 보너스)
                if (stats.activeTree.HasValue)
                    ApplyTreeMutationBonus(stats.activeTree.Value, 3);
                else
                {
                    stats.infectionSpeed      = Mathf.Min(stats.infectionSpeed      + 20f, 100f);
                    stats.networkPenetration  = Mathf.Min(stats.networkPenetration  + 20f, 100f);
                }
                // 3단계 공격 기능 해금
                UnlockFeatureByTree(stats.activeTree);
                Debug.Log("  3단계: 전문화 보너스 + 공격 기능 해금");
                break;

            case 4:
                // 4단계: 폭발적 강화 + 탐지 확률 대폭 상승
                stats.infectionSpeed      = Mathf.Min(stats.infectionSpeed     + 35f, 100f);
                stats.spreadRange         = Mathf.Min(stats.spreadRange        + 30f, 100f);
                stats.networkPenetration  = Mathf.Min(stats.networkPenetration + 25f, 100f);
                // 탐지 위험 폭증 → stealthRate 강제 감소로 표현
                stats.stealthRate = Mathf.Max(stats.stealthRate - 20f, 0f);
                Debug.Log("  4단계: 속도+35 범위+30 침투+25 / 은신-20 (탐지위험 폭증)");
                break;
        }
    }

    // 3단계 트리별 집중 강화
    private void ApplyTreeMutationBonus(UpgradeTree tree, int stage)
    {
        switch (tree)
        {
            case UpgradeTree.Spread:
                // 확산형: 감염속도+40%, 탐지확률+30% (은신 감소로 표현)
                stats.infectionSpeed = Mathf.Min(stats.infectionSpeed + 30f, 100f);
                stats.spreadRange    = Mathf.Min(stats.spreadRange    + 25f, 100f);
                stats.stealthRate    = Mathf.Max(stats.stealthRate    - 15f, 0f);
                break;

            case UpgradeTree.Stealth:
                // 은신형: 탐지회피+50%, 감염속도-20%
                stats.stealthRate      = Mathf.Min(stats.stealthRate    + 35f, 100f);
                stats.infectionSpeed   = Mathf.Max(stats.infectionSpeed - 12f, 0f);
                break;

            case UpgradeTree.Destructive:
                // 파괴형: 감염강도↑, 기관점령 쉬워짐 (강도+30)
                stats.infectionStrength = Mathf.Min(stats.infectionStrength + 30f, 100f);
                stats.spreadRange       = Mathf.Max(stats.spreadRange       -  8f, 0f);
                break;

            case UpgradeTree.Penetration:
                // 침투형: 네트워크침투↑, 일반확산↓
                stats.networkPenetration = Mathf.Min(stats.networkPenetration + 35f, 100f);
                stats.spreadRange        = Mathf.Max(stats.spreadRange        - 10f, 0f);
                break;
        }
    }

    // 3단계 공격 기능 해금
    private void UnlockFeatureByTree(UpgradeTree? tree)
    {
        if (!tree.HasValue) return;
        switch (tree.Value)
        {
            case UpgradeTree.Spread:      stats.unlockedFeatures.Add(AttackFeature.PerformanceDrain); break;
            case UpgradeTree.Stealth:     stats.unlockedFeatures.Add(AttackFeature.SecurityBypass);   break;
            case UpgradeTree.Destructive: stats.unlockedFeatures.Add(AttackFeature.DataCorruption);   break;
            case UpgradeTree.Penetration: stats.unlockedFeatures.Add(AttackFeature.RemoteControl);    break;
        }
    }

    // ============================================================
    //  [2] 업그레이드 트리 선택 (최초 1회)
    //
    //  트리(Tree) 구조 선택 방식:
    //  - 하나 선택 → 일부 계열 잠금
    //  확산형 → 파괴형 일부 제한
    //  은신형 → 속도 계열 제한
    // ============================================================

    // 트리별 코인 비용 (레벨당)
    private static readonly int[] TREE_LEVEL_COST = { 50, 100, 180, 300 };
    // index: 레벨 1,2,3,4

    public bool SelectTree(UpgradeTree tree)
    {
        if (stats.activeTree.HasValue)
        {
            Debug.Log($"[Evolution] 이미 {stats.activeTree.Value} 트리 선택됨. 변경 불가.");
            return false;
        }
        stats.activeTree = tree;
        stats.treeLevel  = 0;
        Debug.Log($"[Evolution] {tree} 트리 선택 완료.");
        return true;
    }

    public bool TryUpgradeTree()
    {
        if (!stats.activeTree.HasValue)
        {
            Debug.Log("[Evolution] 트리를 먼저 선택하세요.");
            return false;
        }

        int nextLv = stats.treeLevel + 1;
        if (nextLv > 4)
        {
            Debug.Log("[Evolution] 트리 최대 레벨입니다.");
            return false;
        }

        int cost = TREE_LEVEL_COST[nextLv - 1];
        if (stats.coins < cost)
        {
            Debug.Log($"[Evolution] 코인 부족. 필요:{cost} 보유:{stats.coins}");
            return false;
        }

        stats.coins -= cost;
        ApplyTreeLevelBonus(stats.activeTree.Value, nextLv);
        stats.treeLevel = nextLv;

        stats.RecalculateDerived();
        infEngine?.OnStatsChanged(stats);

        Debug.Log($"[Evolution] {stats.activeTree.Value} 트리 Lv{nextLv} 완료! {stats}");
        return true;
    }

    // 트리 레벨업 스탯 변화
    private void ApplyTreeLevelBonus(UpgradeTree tree, int lv)
    {
        // 레벨이 높아질수록 효과 증가 (체감 수익 감소 없음 - 전략 확정 단계)
        float gain = lv * 8f;

        switch (tree)
        {
            case UpgradeTree.Spread:
                stats.infectionSpeed = Mathf.Min(stats.infectionSpeed + gain,     100f);
                stats.spreadRange    = Mathf.Min(stats.spreadRange    + gain*0.7f, 100f);
                stats.stealthRate    = Mathf.Max(stats.stealthRate    - gain*0.5f,  0f);
                break;

            case UpgradeTree.Stealth:
                stats.stealthRate      = Mathf.Min(stats.stealthRate    + gain,     100f);
                stats.infectionSpeed   = Mathf.Max(stats.infectionSpeed - gain*0.4f, 0f);
                break;

            case UpgradeTree.Destructive:
                stats.infectionStrength = Mathf.Min(stats.infectionStrength + gain,     100f);
                stats.spreadRange       = Mathf.Max(stats.spreadRange       - gain*0.3f,  0f);
                break;

            case UpgradeTree.Penetration:
                stats.networkPenetration = Mathf.Min(stats.networkPenetration + gain,     100f);
                stats.spreadRange        = Mathf.Max(stats.spreadRange        - gain*0.25f, 0f);
                break;
        }
    }

    // ============================================================
    //  [3] 랜덤 이벤트 변이
    //  감염 설계 "④ 랜덤 이벤트 변이 (선택형)" 구현
    //  예: "급속 변이 발생 → 감염속도 +70% 대신 탐지확률 증가 선택?"
    // ============================================================

    [System.Serializable]
    public class RandomMutationEvent
    {
        public string description;
        public float  infSpeedDelta;
        public float  spreadDelta;
        public float  stealthDelta;
        public float  strengthDelta;
        public float  penetrationDelta;
    }

    // 미리 정의된 랜덤 변이 풀
    private static readonly RandomMutationEvent[] RANDOM_EVENTS =
    {
        new RandomMutationEvent {
            description      = "급속 변이: 감염속도 +35% 대신 탐지확률 급증",
            infSpeedDelta    = 35f,
            stealthDelta     = -20f
        },
        new RandomMutationEvent {
            description      = "은밀 변이: 탐지회피 +30% 대신 확산 속도 감소",
            stealthDelta     = 30f,
            infSpeedDelta    = -15f,
            spreadDelta      = -10f
        },
        new RandomMutationEvent {
            description      = "파괴 변이: 감염강도 +30% 대신 즉각 대응 유발",
            strengthDelta    = 30f,
            stealthDelta     = -15f
        },
        new RandomMutationEvent {
            description      = "침투 변이: 네트워크침투 +30% 대신 일반확산 약화",
            penetrationDelta = 30f,
            spreadDelta      = -12f
        }
    };

    // UI에서 "수락" 버튼을 누르면 호출
    public RandomMutationEvent GetRandomEvent()
    {
        int idx = Random.Range(0, RANDOM_EVENTS.Length);
        return RANDOM_EVENTS[idx];
    }

    public void AcceptRandomMutation(RandomMutationEvent evt)
    {
        stats.infectionSpeed     = Mathf.Clamp(stats.infectionSpeed     + evt.infSpeedDelta,    0f, 100f);
        stats.spreadRange        = Mathf.Clamp(stats.spreadRange        + evt.spreadDelta,       0f, 100f);
        stats.stealthRate        = Mathf.Clamp(stats.stealthRate        + evt.stealthDelta,      0f, 100f);
        stats.infectionStrength  = Mathf.Clamp(stats.infectionStrength  + evt.strengthDelta,    0f, 100f);
        stats.networkPenetration = Mathf.Clamp(stats.networkPenetration + evt.penetrationDelta, 0f, 100f);

        stats.RecalculateDerived();
        infEngine?.OnStatsChanged(stats);
        Debug.Log($"[Evolution] 랜덤 변이 수락: {evt.description}");
    }

    // ============================================================
    //  UI용 헬퍼
    // ============================================================

    // 다음 변이 단계 비용 조회
    public int GetNextMutationCost()
    {
        int s = stats.mutationStage;
        if (s >= PlayerStats.MAX_MUTATION) return -1;
        return MUTATION_COST[s - 1];
    }

    // 다음 트리 레벨 비용 조회
    public int GetNextTreeCost()
    {
        if (!stats.activeTree.HasValue) return -1;
        int nextLv = stats.treeLevel + 1;
        if (nextLv > 4) return -1;
        return TREE_LEVEL_COST[nextLv - 1];
    }

    // 트리 잠금 여부 확인 (트리 선택 시 일부 제한)
    public bool IsTreeLocked(UpgradeTree tree)
    {
        if (!stats.activeTree.HasValue) return false;
        var active = stats.activeTree.Value;
        // 확산형 → 파괴형 일부 제한 (레벨 3 이상)
        if (active == UpgradeTree.Spread && tree == UpgradeTree.Destructive && stats.treeLevel >= 3)
            return true;
        // 은신형 → 확산형 제한 (레벨 2 이상)
        if (active == UpgradeTree.Stealth && tree == UpgradeTree.Spread && stats.treeLevel >= 2)
            return true;
        return false;
    }
}
