using UnityEngine;

public class CureManager : MonoBehaviour
{
    public static CureManager Instance;

    [Header("발각 진행도")]
    [Range(0f, 100f)]
    public float cureProgress = 0f;
    public float baseCureSpeed = 0.2f;

    [Header("방어 시작 조건")]
    [Range(0f, 1f)]
    public float infectionThreshold = 0.60f; // 60% 감염(6/9 구역) 시 방어 시작
    private bool cureStarted = false;
    private bool warningShown = false;

    [Header("스탯")]
    public float stealth = 0f;

    [Header("낮/밤 배율")]
    public float daySpeedMultiplier = 1.3f;
    public float nightSpeedMultiplier = 0.5f;

    [Header("이벤트 발동 여부")]
    private bool phase1Triggered = false;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private bool gameOverCalled = false;

    private float cureSuppressionTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;
        if (gameOverCalled) return;

        // ── 발각 시작 조건 체크 ──
        // 감염된 구역 비율이 threshold(35%)를 넘으면 최초 경고 팝업 표시
        float infectionRate = (float)GameManager.Instance.infectedRegions
                            / GameManager.Instance.totalRegions;

        if (!warningShown && infectionRate >= infectionThreshold)
        {
            warningShown = true;
            UIManager.Instance?.ShowWarning("⚠️ 경보: 다수 구역 침해 감지!\n화이트해커 방어팀이 긴급 소집되었습니다...");
            Invoke(nameof(StartCure), 3f);
        }

        // ── 발각도 진행 (cureStarted = true 일 때만) ──
        if (!cureStarted) return;
        if (cureSuppressionTimer > 0f)
        {
            cureSuppressionTimer -= Time.deltaTime;
            return;
        }

        float timeMultiplier = 1f;
        if (TimeManager.instance != null)
        {
            timeMultiplier = TimeManager.instance.isNight
                ? nightSpeedMultiplier
                : daySpeedMultiplier;
        }

        float malwareMult = MalwareSelectionManager.Instance != null
            ? MalwareSelectionManager.Instance.CureSpeedMultiplier : 1f;

        // 감염 구역이 많을수록 방어 속도 가속 (전염병 주식회사 방식)
        float t = Mathf.Clamp01((infectionRate - infectionThreshold) / (1f - infectionThreshold));
        float infectionBonus = Mathf.Lerp(0f, 0.6f, t);

        float speed = (baseCureSpeed + infectionBonus - stealth * 0.003f) * timeMultiplier * malwareMult;
        speed = Mathf.Max(0.05f, speed);

        cureProgress += speed * Time.deltaTime;
        cureProgress = Mathf.Clamp(cureProgress, 0f, 100f);

        TriggerPhaseEvents();

        if (cureProgress >= 100f && !gameOverCalled)
        {
            gameOverCalled = true;
<<<<<<< Updated upstream
            UIManager.Instance?.ShowWarning("발각도 100% — 바이러스 완전 차단! 게임 오버...");
            GameManager.Instance?.GameOver();
=======
            UIManager.Instance?.ShowWarning("방어 100% — 바이러스 완전 차단! 게임 오버...");
            GameManager.Instance.GameOver();
>>>>>>> Stashed changes
        }
    }

    void StartCure()
    {
        cureStarted = true;
<<<<<<< Updated upstream
        UIManager.Instance?.ShowWarning("🚨 보안 당국 추적 시작! 발각도가 상승합니다.");
=======
        UIManager.Instance?.ShowWarning("🚨 화이트해커 방어 진행 시작!");
>>>>>>> Stashed changes
    }

    void TriggerPhaseEvents()
    {
        if (!phase1Triggered && cureProgress >= 30f)
        {
            phase1Triggered = true;
<<<<<<< Updated upstream
            UIManager.Instance?.ShowWarning("⚠️ 발각도 30% — 백신 프로토타입 개발 시작!\n더 많은 구역을 빠르게 감염시키세요.");
=======
            baseCureSpeed += 0.10f;
            UIManager.Instance?.ShowWarning("⚠️ 방어 30% — 화이트해커 대응팀 편성 완료!\n방어 속도가 증가합니다. 서둘러 전파하세요.");
>>>>>>> Stashed changes
        }
        if (!phase2Triggered && cureProgress >= 60f)
        {
            phase2Triggered = true;
<<<<<<< Updated upstream
            UIManager.Instance?.ShowWarning("⚠️ 발각도 60% — 방화벽 구축 시작!\n일부 구역 접근이 차단됩니다.");
=======
            baseCureSpeed += 0.15f;
            UIManager.Instance?.ShowWarning("⚠️ 방어 60% — 긴급 방어 프로토콜 가동!\n역추적 시스템이 활성화됩니다.");
>>>>>>> Stashed changes
        }
        if (!phase3Triggered && cureProgress >= 90f)
        {
            phase3Triggered = true;
            stealth -= 10f;
<<<<<<< Updated upstream
            UIManager.Instance?.ShowWarning("🚨 발각도 90% — 포렌식 감시 시작!\n은신도 -10. 거의 발각 직전입니다!");
=======
            UIManager.Instance?.ShowWarning("🚨 방어 90% — 포렌식 전면 분석 시작!\n은신도 -10. 바이러스 제거가 임박했습니다!");
>>>>>>> Stashed changes
        }
    }

    public void SuppressCure(float seconds)
    {
        cureSuppressionTimer = Mathf.Max(cureSuppressionTimer, seconds);
    }

    public void OnRegionInfected()
    {
        baseCureSpeed += 0.04f;
    }

    // ── 세이브/로드 ───────────────────────────────────────────────
    public void FillSaveData(SaveData data)
    {
        data.cureProgress        = cureProgress;
        data.baseCureSpeed       = baseCureSpeed;
        data.cureManagerStealth  = stealth;
        data.cureStarted         = cureStarted;
        data.warningShown        = warningShown;
        data.phase1Triggered     = phase1Triggered;
        data.phase2Triggered     = phase2Triggered;
        data.phase3Triggered     = phase3Triggered;
        data.cureSuppressionTimer = cureSuppressionTimer;
    }

    public void ApplyLoadData(SaveData data)
    {
        cureProgress          = data.cureProgress;
        baseCureSpeed         = data.baseCureSpeed;
        stealth               = data.cureManagerStealth;
        cureStarted           = data.cureStarted;
        warningShown          = data.warningShown;
        phase1Triggered       = data.phase1Triggered;
        phase2Triggered       = data.phase2Triggered;
        phase3Triggered       = data.phase3Triggered;
        cureSuppressionTimer  = data.cureSuppressionTimer;
        gameOverCalled        = false;
        CancelInvoke(nameof(StartCure));
    }

    public void UpdateStealth(float newStealth)
    {
        stealth = newStealth;
    }

    public void ResetCure()
    {
        cureProgress = 0f;
        baseCureSpeed = 0.2f;
        stealth = 0f;
        cureStarted = false;
        warningShown = false;
        gameOverCalled = false;
        phase1Triggered = false;
        phase2Triggered = false;
        phase3Triggered = false;
        cureSuppressionTimer = 0f;
        CancelInvoke(nameof(StartCure));
    }
}
