using UnityEngine;

public class CureManager : MonoBehaviour
{
    public static CureManager Instance;

    [Header("발각 진행도")]
    [Range(0f, 100f)]
    public float cureProgress = 0f;
    public float baseCureSpeed = 0.2f;

    [Header("발각 시작 조건")]
    [Range(0f, 1f)]
    public float infectionThreshold = 0.35f; // 35% 감염 시 발각 시작 (Inspector에서 0.3~0.4 조정)
    private bool cureStarted = false;         // 발각도 진행 시작 여부
    private bool warningShown = false;        // 최초 경고 팝업 표시 여부

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

            // UIManager에 경고 팝업 3초 표시
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWarning(
                    "⚠️ 경고: 해킹 징후가 감지되었습니다!\n보안 당국이 조사를 시작합니다...");

            // 3초 후 발각도 진행 시작
            Invoke(nameof(StartCure), 3f);

            Debug.Log("⚠️ 감염률 35% 돌파! 3초 후 발각도 시작!");
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
        float speed = (baseCureSpeed - (stealth * 0.003f)) * timeMultiplier * malwareMult;
        speed = Mathf.Max(0.05f, speed);

        cureProgress += speed * Time.deltaTime;
        cureProgress = Mathf.Clamp(cureProgress, 0f, 100f);

        TriggerPhaseEvents();

        if (cureProgress >= 100f && !gameOverCalled)
        {
            gameOverCalled = true;
            Debug.Log("발각도 100%! GameOver 호출!");
            GameManager.Instance.GameOver();
        }
    }

    // 3초 후 호출되는 함수
    void StartCure()
    {
        cureStarted = true;
        Debug.Log("🚨 발각도 상승 시작!");
    }

    void TriggerPhaseEvents()
    {
        if (!phase1Triggered && cureProgress >= 30f)
        {
            phase1Triggered = true;
            Debug.Log("⚠️ [발각도 30%] 백신 프로토타입 개발 시작!");
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWarning(
                    "⚠️ 발각도 30% — 백신 프로토타입 개발 시작!\n더 많은 구역을 빠르게 감염시키세요.");
            else
                Debug.LogError("[CureManager] UIManager.Instance가 null입니다. 경고 팝업을 표시할 수 없습니다.");
        }
        if (!phase2Triggered && cureProgress >= 60f)
        {
            phase2Triggered = true;
            Debug.Log("⚠️ [발각도 60%] 방화벽 구축 시작!");
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWarning(
                    "⚠️ 발각도 60% — 방화벽 구축 시작!\n일부 구역 접근이 차단됩니다.");
            else
                Debug.LogError("[CureManager] UIManager.Instance가 null입니다. 경고 팝업을 표시할 수 없습니다.");
        }
        if (!phase3Triggered && cureProgress >= 90f)
        {
            phase3Triggered = true;
            stealth -= 10f;
            Debug.Log("🚨 [발각도 90%] 포렌식 감시 시작! 은신도 -10");
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWarning(
                    "🚨 발각도 90% — 포렌식 감시 시작!\n은신도 -10. 거의 발각 직전입니다!");
            else
                Debug.LogError("[CureManager] UIManager.Instance가 null입니다. 경고 팝업을 표시할 수 없습니다.");
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
