using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("코인 UI")]
    public Text coinText;

    [Header("발각도 UI")]
    public Slider cureSlider;
    public Text cureText;
    public Image cureBarFill;

    [Header("스탯 UI")]
    public Text infText;
    public Text compText;
    public Text stealthText;

    [Header("화이트해커 UI")]
    public Slider whiteHackerSlider;
    public Text whiteHackerTargetText;

    [Header("경고 UI")]
    public GameObject warningPanel;
    public Text warningText;
    private float warningTimer = 0f;
    private float warningDuration = 3f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;

        UpdateCureUI();
        UpdateCoinUI();
        UpdateStatUI();
        UpdateWhiteHackerUI();
        UpdateWarning();
    }

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
            if (progress < 30f)
                cureBarFill.color = Color.green;
            else if (progress < 60f)
                cureBarFill.color = Color.yellow;
            else if (progress < 90f)
                cureBarFill.color = new Color(1f, 0.5f, 0f);
            else
                cureBarFill.color = Color.red;
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
        if (infText != null)
            infText.text = $"전염도: {PlayerStats.Instance.inf}";
        if (compText != null)
            compText.text = $"복잡도: {PlayerStats.Instance.comp}";
        if (stealthText != null)
            stealthText.text = $"은신도: {PlayerStats.Instance.stealth}";
    }

    void UpdateWhiteHackerUI()
    {
        if (WhiteHackerManager.Instance == null) return;

        float progress = WhiteHackerManager.Instance.GetRegionCureProgress();
        string target = WhiteHackerManager.Instance.GetTargetRegion();

        if (whiteHackerSlider != null)
            whiteHackerSlider.value = progress;

        if (whiteHackerTargetText != null)
        {
            whiteHackerTargetText.text = string.IsNullOrEmpty(target)
                ? "화이트해커: 대기중"
                : $"화이트해커: [{target}] 치료중...";
        }
    }

    void UpdateWarning()
    {
        if (warningPanel == null) return;
        if (warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f)
                warningPanel.SetActive(false);
        }
    }

    public void ShowWarning(string message)
    {
        if (warningPanel == null || warningText == null) return;
        warningText.text = message;
        warningPanel.SetActive(true);
        warningTimer = warningDuration;
    }

    public void ResetUI()
    {
        if (cureSlider != null) cureSlider.value = 0f;
        if (cureText != null) cureText.text = "발각도 0%";
        if (coinText != null) coinText.text = "💰 100";
        if (warningPanel != null) warningPanel.SetActive(false);
    }
}
