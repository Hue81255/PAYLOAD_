// =============================================
// InfectionEngine.cs  (v2)
// 확산 확률 계산, 단계별 감염 구조, 치료 이벤트, 시간성 반영
// =============================================

using UnityEngine;

public class InfectionEngine : MonoBehaviour
{
    private PlayerStats stats;

    // ── 치료 진행도 (0~100, 100이면 게임오버) ──
    [HideInInspector] public float treatmentProgress = 0f;
    private bool treatmentActive = false;

    // ── 시간 경과 (단계별 속도 변화용) ──
    private float elapsedTime = 0f;

    // ── 백신 이벤트 발동 여부 트래커 ──
    private bool vaccineEventFired    = false;
    private bool firewallEventFired   = false;
    private bool forensicEventFired   = false;

    // ============================================================
    //  EvolutionManager → InfectionEngine 연동 포인트
    // ============================================================
    public void OnStatsChanged(PlayerStats newStats)
    {
        stats = newStats;
        Debug.Log($"[InfectionEngine] 스탯 업데이트: {newStats}");
    }

    // ============================================================
    //  확산 확률 계산
    //  감염 설계 "확산 확률 모델" 완전 구현
    //  확산확률 = 기본확산률 × (1-보안수준) × 연결강도 × 진화보너스
    // ============================================================
    public float CalcSpreadChance(float regionSecurity, float connectionStrength)
    {
        return stats.CalcSpreadChance(regionSecurity, connectionStrength);
    }

    // ============================================================
    //  침투 가능 여부 판단
    //  기획서 [방어스탯]: 최소전염·복잡·은신 체크
    //  감염 설계 v2에서는 5대 스탯으로 확장
    // ============================================================
    public bool CanInfect(TargetDefenseStats target)
    {
        bool speedOk  = stats.infectionSpeed     >= target.minInfectionSpeed;
        bool stealthOk= stats.stealthRate        >= target.minStealth;
        bool strengthOk= stats.infectionStrength >= target.minStrength;
        bool netOk    = stats.networkPenetration >= target.minNetworkPenetration;

        if (!speedOk)
            Debug.Log($"[InfectionEngine] 침투 실패: 감염속도 부족 ({stats.infectionSpeed:F0}<{target.minInfectionSpeed})");
        if (!stealthOk)
            Debug.Log($"[InfectionEngine] 침투 실패: 은신률 부족 ({stats.stealthRate:F0}<{target.minStealth})");
        if (!strengthOk)
            Debug.Log($"[InfectionEngine] 침투 실패: 감염강도 부족 ({stats.infectionStrength:F0}<{target.minStrength})");
        if (!netOk)
            Debug.Log($"[InfectionEngine] 침투 실패: 침투력 부족 ({stats.networkPenetration:F0}<{target.minNetworkPenetration})");

        return speedOk && stealthOk && strengthOk && netOk;
    }

    // ============================================================
    //  해킹 성공 보상 (코인)
    //  기획서: 첫 해킹 대량, 이후 소량
    // ============================================================
    public int CalculateReward(TargetType targetType, bool isFirstTime)
    {
        int base_ = targetType switch
        {
            TargetType.IOT       => 0,
            TargetType.Mobile    => 5,
            TargetType.Corporate => 30,
            TargetType.Boss      => 150,
            _ => 0
        };

        // 데이터 탈취 기능 활성 시 보너스
        if (stats.unlockedFeatures.Contains(AttackFeature.DataTheft))
            base_ = Mathf.RoundToInt(base_ * 1.5f);

        return isFirstTime ? base_ : Mathf.Max(base_ / 5, 1);
    }

    // ============================================================
    //  공격 기능별 효과 적용
    //  감염 설계 "바이러스 공격 기능" 6종
    // ============================================================
    public void ApplyAttackFeature(AttackFeature feature, Region targetRegion)
    {
        if (!stats.unlockedFeatures.Contains(feature))
        {
            Debug.Log($"[InfectionEngine] {feature} 미해금 상태");
            return;
        }

        switch (feature)
        {
            case AttackFeature.DataCorruption:
                // 데이터 손상: 기관 점령 난이도↓, 복구시간↑ (방어 지연)
                targetRegion.defenseMultiplier    *= 0.7f; // 방어력 30% 감소
                targetRegion.recoveryTimeMultiplier *= 1.5f;
                Debug.Log($"[Attack] 데이터 손상: {targetRegion.name} 방어력-30% 복구시간+50%");
                break;

            case AttackFeature.PerformanceDrain:
                // 성능 저하: 감염 확산 속도↑, 보안 반응속도↓
                targetRegion.securityResponseSpeed *= 0.6f;
                targetRegion.spreadBonus           += 0.2f;
                Debug.Log($"[Attack] 성능 저하: {targetRegion.name} 보안반응-40% 확산+20%");
                break;

            case AttackFeature.SecurityBypass:
                // 보안 무력화: 로그삭제 → 탐지확률 감소, 감염지속↑
                targetRegion.detectionModifier     *= 0.5f;
                targetRegion.infectionPersistence  += 0.3f;
                Debug.Log($"[Attack] 보안 무력화: {targetRegion.name} 탐지확률-50%");
                break;

            case AttackFeature.DataTheft:
                // 데이터 탈취: 코인 보너스 (CalculateReward에서 처리)
                Debug.Log($"[Attack] 데이터 탈취: {targetRegion.name} 코인 획득 보너스 활성");
                break;

            case AttackFeature.RemoteControl:
                // 원격 제어: 확산 경로 강제 생성, 기관 공격 속도↑
                targetRegion.forceSpreadActive = true;
                targetRegion.institutionAttackSpeedBonus += 0.4f;
                Debug.Log($"[Attack] 원격 제어: {targetRegion.name} 강제 확산 활성");
                break;

            case AttackFeature.Ransomware:
                // 랜섬 공격: 기관 즉시 마비 + 점령 진행도↑ + 탐지확률↑↑
                targetRegion.infectionRate   = 1.0f; // 감염 즉시 최대
                targetRegion.detectionModifier *= 3.0f; // 탐지위험 3배
                Debug.Log($"[Attack] 랜섬 공격: {targetRegion.name} 즉시 마비! 탐지위험 3배");
                break;
        }
    }

    // ============================================================
    //  시간 경과 반영 (감염 설계 "시간성")
    //  초반 느림 → 중반 급속 증가 → 후반 방어 활성화 감소
    // ============================================================
    private float GetTimeSpeedMultiplier()
    {
        // 초반(0~120초): 0.5~1.0배
        // 중반(120~300초): 1.0~2.0배 급증
        // 후반(300초~): 2.0배에서 방어 활성화로 감소
        if (elapsedTime < 120f)
            return Mathf.Lerp(0.5f, 1.0f, elapsedTime / 120f);
        else if (elapsedTime < 300f)
            return Mathf.Lerp(1.0f, 2.0f, (elapsedTime - 120f) / 180f);
        else
            return Mathf.Max(2.0f - (elapsedTime - 300f) / 200f, 1.0f);
    }

    // ============================================================
    //  치료 이벤트 시스템
    //  기획서: 백신(1~30%), 방화벽(31~60%), 포렌식(61~90%)
    // ============================================================
    public void StartTreatment()
    {
        treatmentActive   = true;
        treatmentProgress = 0f;
        Debug.Log("[InfectionEngine] 치료 시작!");
    }

    private void HandleTreatmentEvents()
    {
        if (treatmentProgress >= 1f && treatmentProgress <= 30f && !vaccineEventFired)
        {
            vaccineEventFired = true;
            OnVaccinePrototype();
        }
        else if (treatmentProgress > 30f && treatmentProgress <= 60f && !firewallEventFired)
        {
            firewallEventFired = true;
            OnFirewallBuilt();
        }
        else if (treatmentProgress > 60f && treatmentProgress <= 90f && !forensicEventFired)
        {
            forensicEventFired = true;
            OnForensicMonitoring();
        }
    }

    // 백신 프로토타입: 일부 감염 제거, 전파력 감소
    private void OnVaccinePrototype()
    {
        stats.infectionSpeed = Mathf.Max(stats.infectionSpeed - 8f, 0f);
        stats.RecalculateDerived();
        Debug.Log("[Event] 백신 프로토타입 등장! 감염속도-8");
    }

    // 방화벽 구축: 특정 구역 감염 저지 → InfectionEngine에서 지역 보안 수치 상승으로 처리
    private void OnFirewallBuilt()
    {
        // 실제 지역 보안 수치 상승은 RegionManager에서 처리
        // 여기서는 이벤트만 발생시켜 연동
        Debug.Log("[Event] 방화벽 구축! RegionManager에 보안 강화 요청");
        // RegionManager.Instance?.TriggerFirewallEvent(); // 팀원 코드 연동 포인트
    }

    // 포렌식 감시: 은신률 하락, 백신 개발 가속
    private void OnForensicMonitoring()
    {
        stats.stealthRate = Mathf.Max(stats.stealthRate - 12f, 0f);
        stats.RecalculateDerived();
        Debug.Log("[Event] 포렌식 감시! 은신률-12, 백신개발 가속");
    }

    // ============================================================
    //  매 프레임 업데이트
    // ============================================================
    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (!treatmentActive) return;

        // 치료 진행: 기본속도 × 시간배율 × (1 - 치료속도감소율)
        float baseTreatmentSpeed = 2.0f; // 초당 2%
        float timeMultiplier     = GetTimeSpeedMultiplier();
        float effectiveSpeed     = baseTreatmentSpeed * timeMultiplier * (1f - stats.treatmentSlowdown);

        treatmentProgress += effectiveSpeed * Time.deltaTime;
        treatmentProgress  = Mathf.Clamp(treatmentProgress, 0f, 100f);

        HandleTreatmentEvents();

        if (treatmentProgress >= 100f)
        {
            treatmentActive = false;
            Debug.Log("[InfectionEngine] 치료 100% → 게임오버!");
            // TODO: GameManager.Instance?.GameOver();
        }
    }
}

// ============================================================
//  보조 데이터 구조: 침투 대상 방어 스탯 (v2 확장)
// ============================================================
[System.Serializable]
public class TargetDefenseStats
{
    public float minInfectionSpeed;     // 최소 감염 속도
    public float minStealth;            // 최소 은신률
    public float minStrength;           // 최소 감염 강도
    public float minNetworkPenetration; // 최소 네트워크 침투력
}

// 침투 대상 타입
public enum TargetType
{
    IOT,        // IOT 기기: 리워드 없음, 발판 역할
    Mobile,     // 모바일 기기: 허들 낮음
    Corporate,  // 일반 기업/기관
    Boss        // 대기업/공공기관
}

// ============================================================
//  지역(구/군) 데이터 구조 뼈대
//  팀원의 RegionManager와 연동될 부분
// ============================================================
[System.Serializable]
public class Region
{
    public string name;

    // 기획서 "지역별 특성"
    public float securityLevel;         // 보안 수준 (0~1)
    public float connectionStrength;    // 인접 지역 연결 강도

    // 공격 기능에 의해 변하는 수치들
    public float defenseMultiplier       = 1.0f;
    public float recoveryTimeMultiplier  = 1.0f;
    public float securityResponseSpeed   = 1.0f;
    public float spreadBonus             = 0f;
    public float detectionModifier       = 1.0f;
    public float infectionPersistence    = 0f;
    public bool  forceSpreadActive       = false;
    public float institutionAttackSpeedBonus = 0f;

    // 감염 진행도 (0~1, 1이면 구역 점령)
    public float infectionRate = 0f;

    // 보스 침투 가능 여부 (감염도 임계치 도달 시)
    public bool IsBossUnlocked => infectionRate >= 0.7f;
}
