using UnityEngine;
using UnityEngine.UI;

public class EvolutionManager : MonoBehaviour
{
    public static EvolutionManager Instance;

    // ── 업그레이드 설정 ──────────────────────────────────────────
    [Header("업그레이드 설정")]
    public int maxLevel = 10;

    [Tooltip("전염도: 레벨 × 이 값만큼 코인 소모")]
    public int infBaseCost = 50;
    [Tooltip("복잡도: 레벨 × 이 값만큼 코인 소모")]
    public int compBaseCost = 50;
    [Tooltip("은신도: 레벨 × 이 값만큼 코인 소모")]
    public int stealthBaseCost = 40;

    [Tooltip("전염도 업그레이드 1회당 스탯 증가량")]
    public int infStatGain = 8;
    [Tooltip("복잡도 업그레이드 1회당 스탯 증가량")]
    public int compStatGain = 8;
    [Tooltip("은신도 업그레이드 1회당 스탯 증가량")]
    public int stealthStatGain = 5;

    // ── 현재 업그레이드 레벨 (Inspector 확인용) ──────────────────
    [Header("현재 레벨 (읽기 전용)")]
    public int infLevel = 1;
    public int compLevel = 1;
    public int stealthLevel = 1;

    // ── 업그레이드 UI – 전염도 ───────────────────────────────────
    [Header("UI – 전염도 업그레이드")]
    public Button  infUpgradeButton;
    public Text    infLevelText;   // 예: "Lv. 3 / 10"
    public Text    infCostText;    // 예: "150 코인"
    public Text    infStatText;    // 예: "전염도: 34"

    // ── 업그레이드 UI – 복잡도 ───────────────────────────────────
    [Header("UI – 복잡도 업그레이드")]
    public Button  compUpgradeButton;
    public Text    compLevelText;
    public Text    compCostText;
    public Text    compStatText;

    // ── 업그레이드 UI – 은신도 ───────────────────────────────────
    [Header("UI – 은신도 업그레이드")]
    public Button  stealthUpgradeButton;
    public Text    stealthLevelText;
    public Text    stealthCostText;
    public Text    stealthStatText;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()  => GlobalEventManager.OnHackSuccess += OnHackSuccess;
    void OnDisable() => GlobalEventManager.OnHackSuccess -= OnHackSuccess;

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;
        RefreshUpgradeUI();
    }

    // ── 해킹 보상 → PlayerStats.coins에 직접 적립 ────────────────
    void OnHackSuccess(string id, int reward)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddCoins(reward);
    }

    // ── 업그레이드 버튼 콜백 ─────────────────────────────────────

    public void UpgradeInf()
    {
        if (!CanUpgrade(infLevel, infBaseCost)) return;

        int cost = GetCost(infLevel, infBaseCost);
        PlayerStats.Instance.AddCoins(-cost);
        PlayerStats.Instance.UpgradeInf(infStatGain);
        infLevel++;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    public void UpgradeComp()
    {
        if (!CanUpgrade(compLevel, compBaseCost)) return;

        int cost = GetCost(compLevel, compBaseCost);
        PlayerStats.Instance.AddCoins(-cost);
        PlayerStats.Instance.UpgradeComp(compStatGain);
        compLevel++;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    public void UpgradeStealth()
    {
        if (!CanUpgrade(stealthLevel, stealthBaseCost)) return;

        int cost = GetCost(stealthLevel, stealthBaseCost);
        PlayerStats.Instance.AddCoins(-cost);
        PlayerStats.Instance.UpgradeStealth(stealthStatGain);
        stealthLevel++;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    // ── 게임 재시작 시 레벨 초기화 ───────────────────────────────
    public void ResetLevels()
    {
        infLevel    = 1;
        compLevel   = 1;
        stealthLevel = 1;
        RefreshUpgradeUI();
    }

    // ─────────────────────────────────────────────────────────────
    // 내부 유틸
    // ─────────────────────────────────────────────────────────────

    // 현재 레벨에서 다음 레벨까지의 비용: baseCost × currentLevel
    int GetCost(int currentLevel, int baseCost) => baseCost * currentLevel;

    bool CanUpgrade(int currentLevel, int baseCost)
    {
        if (currentLevel >= maxLevel) return false;
        if (PlayerStats.Instance == null) return false;
        return PlayerStats.Instance.coins >= GetCost(currentLevel, baseCost);
    }

    void RefreshUpgradeUI()
    {
        if (PlayerStats.Instance == null) return;
        int coins = PlayerStats.Instance.coins;

        UpdateStatUI(infUpgradeButton,    infLevelText,    infCostText,    infStatText,
                     infLevel,    infBaseCost,    PlayerStats.Instance.inf,    "전염도");

        UpdateStatUI(compUpgradeButton,   compLevelText,   compCostText,   compStatText,
                     compLevel,   compBaseCost,   PlayerStats.Instance.comp,   "복잡도");

        UpdateStatUI(stealthUpgradeButton, stealthLevelText, stealthCostText, stealthStatText,
                     stealthLevel, stealthBaseCost, PlayerStats.Instance.stealth, "은신도");
    }

    void UpdateStatUI(Button btn, Text levelTxt, Text costTxt, Text statTxt,
                      int level, int baseCost, int statValue, string statName)
    {
        bool maxed = level >= maxLevel;
        int  cost  = GetCost(level, baseCost);
        int  coins = PlayerStats.Instance != null ? PlayerStats.Instance.coins : 0;

        if (levelTxt != null)
            levelTxt.text = maxed ? $"Lv. MAX" : $"Lv. {level} / {maxLevel}";

        if (costTxt != null)
            costTxt.text = maxed ? "최대 레벨" : $"{cost} 코인";

        if (statTxt != null)
            statTxt.text = $"{statName}: {statValue}";

        if (btn != null)
            btn.interactable = !maxed && coins >= cost;
    }
}
