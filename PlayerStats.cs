// =============================================
// PlayerStats.cs  (v2)
// 플레이어의 5대 스탯, 변이 단계, 업그레이드 트리 상태 관리
// =============================================

using System.Collections.Generic;
using UnityEngine;

// ── 악성코드 타입 ──
public enum MalwareType
{
    Virus,      // 균형형
    Worm,       // 네트워크 자동확산 특화
    Spyware,    // 탐지회피 특화
    Stealer,    // 데이터탈취 / 전략성
    Keylogger   // 잠복형
}

// ── 기본 감염 루트 ──
public enum InfectionMethod
{
    Email,      // 전파속도 보통, 초기 확산 안정
    Download,   // 감염강도 높음, 사용자 행동 의존
    Network,    // 빠른 집단 감염, 보안 수준 영향 큼
    USB         // 느리지만 특정 기관 침투 강함
}

// ── 업그레이드 트리 ──
public enum UpgradeTree
{
    Spread,         // 확산형:  감염속도↑ 확산범위↑  / 탐지위험↑
    Stealth,        // 은신형:  탐지회피↑ 지속시간↑  / 확산속도↓
    Destructive,    // 파괴형:  감염강도↑ 데이터손상↑ / 즉시대응 유도
    Penetration     // 침투형:  네트워크침투↑ 기관공격↑ / 일반확산 약함
}

// ── 바이러스 공격 기능 ──
public enum AttackFeature
{
    DataCorruption,     // 데이터 손상:  확산↓  파괴↑↑
    PerformanceDrain,   // 성능 저하:   확산 중~높  파괴 중
    SecurityBypass,     // 보안 무력화: 확산↑  직접피해↓
    DataTheft,          // 데이터 탈취: 확산 중  전략성↑
    RemoteControl,      // 원격 제어:   확산↑  전략성↑↑
    Ransomware          // 랜섬 공격:   파괴↑↑  탐지위험↑↑
}

[System.Serializable]
public class PlayerStats
{
    // ────────────────────────────────────────
    //  5대 핵심 업그레이드 스탯 (0~100)
    // ────────────────────────────────────────
    public float infectionSpeed;     // ① 감염 속도
    public float spreadRange;        // ② 확산 범위
    public float stealthRate;        // ③ 탐지 회피 능력
    public float infectionStrength;  // ④ 감염 강도
    public float networkPenetration; // ⑤ 네트워크 침투력

    // ────────────────────────────────────────
    //  파생 스탯 (RecalculateDerived()로 자동 갱신)
    // ────────────────────────────────────────
    [HideInInspector] public float detectionChance;   // 탐지 확률 (0~1)
    [HideInInspector] public float treatmentSlowdown; // 치료속도 감소율 (0~1)

    // ────────────────────────────────────────
    //  변이 단계 (1~4)
    // ────────────────────────────────────────
    public int mutationStage = 1;
    public const int MAX_MUTATION = 4;

    // ────────────────────────────────────────
    //  업그레이드 트리
    // ────────────────────────────────────────
    public UpgradeTree? activeTree = null; // null = 아직 미선택
    public int treeLevel = 0;              // 선택된 트리 내 레벨 (0~4)

    // ────────────────────────────────────────
    //  활성 공격 기능 목록
    // ────────────────────────────────────────
    public HashSet<AttackFeature> unlockedFeatures = new HashSet<AttackFeature>();

    // ────────────────────────────────────────
    //  조건 해금 트래커
    // ────────────────────────────────────────
    public int   capturedInstitutions = 0;  // 점령 기관 수
    public float maxInfectionRate     = 0f; // 달성 최고 감염률
    public float maxStealthDuration   = 0f; // 탐지 회피 유지 최장 시간(초)

    // ────────────────────────────────────────
    //  코인 / 선택 정보
    // ────────────────────────────────────────
    public int coins;
    public MalwareType    malwareType;
    public InfectionMethod infectionMethod;

    // ============================================================
    //  생성자: 타입별 기본 스탯 + 감염 방식 보너스
    // ============================================================
    public PlayerStats(MalwareType type, InfectionMethod method)
    {
        malwareType     = type;
        infectionMethod = method;
        coins           = 100;

        switch (type)
        {
            case MalwareType.Worm:
                // 네트워크 자동확산 특화 → 속도·범위·침투 높음, 은신 낮음
                infectionSpeed = 40f; spreadRange = 50f; stealthRate = 15f;
                infectionStrength = 20f; networkPenetration = 45f;
                break;

            case MalwareType.Spyware:
                // 탐지회피 특화 → 은신 높음, 확산 낮음
                infectionSpeed = 20f; spreadRange = 25f; stealthRate = 55f;
                infectionStrength = 20f; networkPenetration = 25f;
                break;

            case MalwareType.Stealer:
                // 데이터탈취 / 전략성 → 균형+강도
                infectionSpeed = 25f; spreadRange = 30f; stealthRate = 30f;
                infectionStrength = 35f; networkPenetration = 35f;
                break;

            case MalwareType.Keylogger:
                // 잠복형 → 초반 느림, 은신·강도 중간
                infectionSpeed = 15f; spreadRange = 20f; stealthRate = 45f;
                infectionStrength = 30f; networkPenetration = 20f;
                break;

            default: // Virus: 균형형
                infectionSpeed = 30f; spreadRange = 30f; stealthRate = 30f;
                infectionStrength = 30f; networkPenetration = 30f;
                break;
        }

        ApplyMethodBonus(method);
        RecalculateDerived();
    }

    // ============================================================
    //  감염 방식 보너스
    // ============================================================
    private void ApplyMethodBonus(InfectionMethod method)
    {
        switch (method)
        {
            case InfectionMethod.Email:
                infectionSpeed += 5f; spreadRange += 5f;
                break;
            case InfectionMethod.Download:
                infectionStrength += 15f;
                break;
            case InfectionMethod.Network:
                networkPenetration += 15f; infectionSpeed += 10f;
                stealthRate -= 5f; // 보안 수준 영향 → 탐지 위험 증가
                break;
            case InfectionMethod.USB:
                networkPenetration += 20f; infectionSpeed -= 10f;
                break;
        }
    }

    // ============================================================
    //  파생 스탯 자동 계산 (스탯 변경 후 반드시 호출)
    // ============================================================
    public void RecalculateDerived()
    {
        // 탐지 확률: 속도↑→위험, 은신↑→안전
        detectionChance = Mathf.Clamp(
            0.05f + (infectionSpeed / 100f) * 0.35f - (stealthRate / 100f) * 0.25f,
            0.01f, 0.95f);

        // 치료 속도 감소율: 강도↑ + 은신↑ → 치료 더 느려짐
        treatmentSlowdown = Mathf.Clamp(
            (infectionStrength / 100f) * 0.5f + (stealthRate / 100f) * 0.3f,
            0f, 0.9f);
    }

    // ============================================================
    //  확산 확률 공식
    //  확산확률 = 기본확산률 × (1-보안수준) × 연결강도 × 진화보너스
    // ============================================================
    public float CalcSpreadChance(float regionSecurity, float connectionStrength)
    {
        float basePct      = infectionSpeed / 100f;
        float secFactor    = 1f - Mathf.Clamp01(regionSecurity);
        float connFactor   = Mathf.Clamp01(connectionStrength);
        float evoBonus     = 1f + (mutationStage - 1) * 0.15f; // 단계당 +15%
        float rangeBonus   = 1f + (spreadRange   / 100f) * 0.3f;

        return Mathf.Clamp01(basePct * secFactor * connFactor * evoBonus * rangeBonus);
    }

    public override string ToString() =>
        $"[{malwareType}/{infectionMethod}] 변이:{mutationStage}단계 | " +
        $"속도:{infectionSpeed:F0} 범위:{spreadRange:F0} 은신:{stealthRate:F0} " +
        $"강도:{infectionStrength:F0} 침투:{networkPenetration:F0} | " +
        $"탐지:{detectionChance*100:F1}% 코인:{coins}";
}
