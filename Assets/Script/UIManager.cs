using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Text infectedCountText;
    public Text spreadTimerText;

    [Header("알림 텍스트 (TMP — 하나만 연결)")]
    public TMP_Text notifyText;
    [Tooltip("알림이 화면에 유지되는 시간(초)")]
    public float warningDuration = 4f;

    private Coroutine _clearCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (notifyText == null)
        {
            Debug.LogWarning("[UIManager] notifyText가 연결되지 않았습니다.");
            return;
        }
        notifyText.text    = "";
        notifyText.color   = Color.white;
        notifyText.fontSize = 24;
        notifyText.gameObject.SetActive(true);
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
    }

    // ── 알림 표시 ─────────────────────────────────────────────────

    public void ShowWarning(string message)
    {
        if (notifyText == null) { Debug.Log($"[알림] {message}"); return; }

        if (_clearCoroutine != null) StopCoroutine(_clearCoroutine);
        notifyText.gameObject.SetActive(true);
        notifyText.text  = message;
        notifyText.color = Color.white;
        _clearCoroutine  = StartCoroutine(ClearAfterDelay());
    }

    IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSecondsRealtime(warningDuration);
        if (notifyText != null) notifyText.text = "";
        _clearCoroutine = null;
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
        if (infText     != null) infText.text     = $"전염도: {PlayerStats.Instance.inf}";
        if (compText    != null) compText.text    = $"복잡도: {PlayerStats.Instance.comp}";
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
        if (_clearCoroutine != null) { StopCoroutine(_clearCoroutine); _clearCoroutine = null; }
        if (notifyText  != null) notifyText.text   = "";
        if (cureSlider  != null) cureSlider.value  = 0f;
        if (cureText    != null) cureText.text      = "발각도 0%";
        if (coinText    != null) coinText.text       = "💰 100";
    }
}
