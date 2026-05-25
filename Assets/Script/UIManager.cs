using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("코인 UI")]
    public Text coinText;

    [Header("발각도 UI")]
    public Slider cureSlider;
    public Text   cureText;
    public Image  cureBarFill;

    [Header("스탯 UI")]
    public Text infText;
    public Text compText;
    public Text stealthText;

    [Header("화이트해커 UI")]
    public Slider whiteHackerSlider;
    public Text   whiteHackerTargetText;

    [Header("감염 현황 UI")]
    public Text infectedCountText;  // "감염 구역: 3 / 9"
    public Text spreadTimerText;    // "다음 전파: 7초"

    [Header("경고 팝업 UI")]
    public GameObject warningPanel;   // 경고 전체 패널
    public Text       warningText;    // 경고 메시지 텍스트
    public Image      warningBg;      // (선택) 배경 이미지 – 단계별 색상 변경
    [Tooltip("경고 하나가 화면에 유지되는 시간(초)")]
    public float warningDuration = 5f;

    // ── 경고 큐 상태 ─────────────────────────────────────────────
    private readonly Queue<string> warningQueue = new Queue<string>();
    private bool  isShowingWarning = false;
    private float warningTimer     = 0f;

    // 단계별 배경 색상
    private static readonly Color ColorInfo    = new Color(0.15f, 0.55f, 1f,  0.92f); // 파랑
    private static readonly Color ColorCaution = new Color(1f,    0.75f, 0f,  0.92f); // 노랑
    private static readonly Color ColorDanger  = new Color(1f,    0.35f, 0f,  0.92f); // 주황
    private static readonly Color ColorCritical= new Color(0.85f, 0.05f, 0.05f,0.92f);// 빨강

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Inspector 연결 누락 조기 감지
        if (warningPanel == null)
            Debug.LogError("[UIManager] warningPanel이 연결되지 않았습니다. Inspector에서 경고 팝업 패널을 연결하세요.");
        if (warningText == null)
            Debug.LogError("[UIManager] warningText가 연결되지 않았습니다. Inspector에서 경고 텍스트를 연결하세요.");

        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    void Update()
    {
        bool gameRunning = GameManager.Instance != null && GameManager.Instance.isGameStarted;

        if (gameRunning)
        {
            UpdateCureUI();
            UpdateCoinUI();
            UpdateStatUI();
            UpdateWhiteHackerUI();
            UpdateInfectionUI();
        }

        // 경고 큐는 게임 상태와 무관하게 항상 처리 (게임 오버 화면에서도 마지막 경고 표시)
        UpdateWarningQueue();
    }

    // ── 경고 큐 처리 ─────────────────────────────────────────────

    void UpdateWarningQueue()
    {
        if (warningPanel == null) return;

        if (isShowingWarning)
        {
            warningTimer -= Time.unscaledDeltaTime;
            if (warningTimer <= 0f)
            {
                isShowingWarning = false;
                warningPanel.SetActive(false);
                TryShowNextWarning();
            }
        }
        else
        {
            TryShowNextWarning();
        }
    }

    void TryShowNextWarning()
    {
        if (warningQueue.Count == 0) return;
        DisplayWarning(warningQueue.Dequeue());
    }

    void DisplayWarning(string message)
    {
        if (warningPanel == null || warningText == null) return;

        warningText.text = message;

        // 메시지 내용으로 위험도 색상 자동 결정
        if (warningBg != null)
            warningBg.color = GetWarningColor(message);

        warningPanel.SetActive(true);
        warningTimer     = warningDuration;
        isShowingWarning = true;
    }

    Color GetWarningColor(string message)
    {
        if (message.Contains("🚨") || message.Contains("포렌식") || message.Contains("위험"))
            return ColorCritical;
        if (message.Contains("방화벽") || message.Contains("차단"))
            return ColorDanger;
        if (message.Contains("⚠️") || message.Contains("백신"))
            return ColorCaution;
        return ColorInfo;
    }

    // ── 외부 호출 진입점 ─────────────────────────────────────────

    /// <summary>
    /// 경고 메시지를 큐에 추가한다. 현재 경고가 표시 중이면 끝난 후 순서대로 표시된다.
    /// </summary>
    public void ShowWarning(string message)
    {
        if (warningPanel == null || warningText == null)
        {
            Debug.LogError($"[UIManager] warningPanel 또는 warningText가 null입니다. Inspector 연결을 확인하세요.\n메시지: {message}");
            return;
        }
        warningQueue.Enqueue(message);
    }

    // ── HUD 업데이트 ─────────────────────────────────────────────

    void UpdateCureUI()
    {
        if (CureManager.Instance == null) return;
        float progress = CureManager.Instance.cureProgress;

        if (cureSlider != null)
            cureSlider.value = progress / 100f;

        if (cureText != null)
            cureText.text = $"발각도 {Mathf.FloorToInt(progress)}%";

        if (cureBarFill != null)
        {
            if      (progress < 30f) cureBarFill.color = Color.green;
            else if (progress < 60f) cureBarFill.color = Color.yellow;
            else if (progress < 90f) cureBarFill.color = new Color(1f, 0.5f, 0f);
            else                     cureBarFill.color = Color.red;
        }
    }

    void UpdateCoinUI()
    {
        if (PlayerStats.Instance == null) return;
        if (coinText != null)
            coinText.text = $"💰 {PlayerStats.Instance.coins}";
    }

    void UpdateStatUI()
    {
        if (PlayerStats.Instance == null) return;
        if (infText    != null) infText.text    = $"전염도: {PlayerStats.Instance.inf}";
        if (compText   != null) compText.text   = $"복잡도: {PlayerStats.Instance.comp}";
        if (stealthText != null) stealthText.text = $"은신도: {PlayerStats.Instance.stealth}";
    }

    void UpdateInfectionUI()
    {
        if (infectedCountText != null && GameManager.Instance != null)
            infectedCountText.text = $"감염 구역: {GameManager.Instance.infectedRegions} / {GameManager.Instance.totalRegions}";

        if (spreadTimerText != null && SpreadManager.Instance != null)
        {
            float t = SpreadManager.Instance.NextSpreadIn;
            spreadTimerText.text = $"다음 전파: {Mathf.CeilToInt(t)}초";
        }
    }

    void UpdateWhiteHackerUI()
    {
        if (WhiteHackerManager.Instance == null) return;

        float progress = WhiteHackerManager.Instance.GetRegionCureProgress();
        string target  = WhiteHackerManager.Instance.GetTargetRegion();

        if (whiteHackerSlider != null)
            whiteHackerSlider.value = progress;

        if (whiteHackerTargetText != null)
        {
            whiteHackerTargetText.text = string.IsNullOrEmpty(target)
                ? "화이트해커: 대기중"
                : $"화이트해커: [{target}] 치료중...";
        }
    }

    public void ResetUI()
    {
        warningQueue.Clear();
        isShowingWarning = false;
        warningTimer     = 0f;

        if (warningPanel != null) warningPanel.SetActive(false);
        if (cureSlider   != null) cureSlider.value  = 0f;
        if (cureText     != null) cureText.text      = "발각도 0%";
        if (coinText     != null) coinText.text       = "💰 100";
    }
}
