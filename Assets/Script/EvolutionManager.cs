using UnityEngine;
using UnityEngine.UI;

public class EvolutionManager : MonoBehaviour
{
    public static EvolutionManager Instance;

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

    [Header("현재 레벨 (읽기 전용)")]
    public int infLevel = 1;
    public int compLevel = 1;
    public int stealthLevel = 1;

    // ── 업그레이드 UI – 전염도 ───────────────────────────────────
    [Header("UI – 전염도")]
    public Button infUpgradeButton;
    public Button infDowngradeButton;
    public Text   infLevelText;
    public Text   infCostText;
    public Text   infStatText;

    // ── 업그레이드 UI – 복잡도 ───────────────────────────────────
    [Header("UI – 복잡도")]
    public Button compUpgradeButton;
    public Button compDowngradeButton;
    public Text   compLevelText;
    public Text   compCostText;
    public Text   compStatText;

    // ── 업그레이드 UI – 은신도 ───────────────────────────────────
    [Header("UI – 은신도")]
    public Button stealthUpgradeButton;
    public Button stealthDowngradeButton;
    public Text   stealthLevelText;
    public Text   stealthCostText;
    public Text   stealthStatText;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()  => GlobalEventManager.OnHackSuccess += OnHackSuccess;
    void OnDisable() => GlobalEventManager.OnHackSuccess -= OnHackSuccess;

    void Update()
    {
        // New main 씬: 게임 진행 중 HUD 갱신
        // Process 씬: GameManager 없으므로 항상 갱신
        if (GameManager.Instance == null || GameManager.Instance.isGameStarted)
            RefreshUpgradeUI();
    }

    void OnHackSuccess(string id, int reward)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddCoins(reward);
    }

    // ── 업그레이드 ────────────────────────────────────────────────

    public void UpgradeInf()
    {
        if (!CanUpgrade(infLevel, infBaseCost)) return;
        PlayerStats.Instance.AddCoins(-GetCost(infLevel, infBaseCost));
        PlayerStats.Instance.UpgradeInf(infStatGain);
        infLevel++;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    public void UpgradeComp()
    {
        if (!CanUpgrade(compLevel, compBaseCost)) return;
        PlayerStats.Instance.AddCoins(-GetCost(compLevel, compBaseCost));
        PlayerStats.Instance.UpgradeComp(compStatGain);
        compLevel++;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    public void UpgradeStealth()
    {
        if (!CanUpgrade(stealthLevel, stealthBaseCost)) return;
        PlayerStats.Instance.AddCoins(-GetCost(stealthLevel, stealthBaseCost));
        PlayerStats.Instance.UpgradeStealth(stealthStatGain);
        stealthLevel++;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    // ── 다운그레이드 (취소 / 환불) ───────────────────────────────

    public void DowngradeInf()
    {
        if (infLevel <= 1 || PlayerStats.Instance == null) return;
        // 이전 업그레이드 비용 전액 환불: GetCost(level-1, base) = base*(level-1)
        PlayerStats.Instance.AddCoins(GetCost(infLevel - 1, infBaseCost));
        PlayerStats.Instance.UpgradeInf(-infStatGain);
        infLevel--;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    public void DowngradeComp()
    {
        if (compLevel <= 1 || PlayerStats.Instance == null) return;
        PlayerStats.Instance.AddCoins(GetCost(compLevel - 1, compBaseCost));
        PlayerStats.Instance.UpgradeComp(-compStatGain);
        compLevel--;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    public void DowngradeStealth()
    {
        if (stealthLevel <= 1 || PlayerStats.Instance == null) return;
        PlayerStats.Instance.AddCoins(GetCost(stealthLevel - 1, stealthBaseCost));
        PlayerStats.Instance.UpgradeStealth(-stealthStatGain);
        stealthLevel--;
        RefreshUpgradeUI();
        SaveManager.Instance?.Save();
    }

    // ── 게임 재시작 시 레벨 초기화 ───────────────────────────────

    public void ResetLevels()
    {
        infLevel     = 1;
        compLevel    = 1;
        stealthLevel = 1;
        RefreshUpgradeUI();
    }

    // ─────────────────────────────────────────────────────────────

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

        UpdateStatUI(infUpgradeButton,    infDowngradeButton,    infLevelText,    infCostText,    infStatText,
                     infLevel,    infBaseCost,    infStatGain,    PlayerStats.Instance.inf,    "전염도");

        UpdateStatUI(compUpgradeButton,   compDowngradeButton,   compLevelText,   compCostText,   compStatText,
                     compLevel,   compBaseCost,   compStatGain,   PlayerStats.Instance.comp,   "복잡도");

        UpdateStatUI(stealthUpgradeButton, stealthDowngradeButton, stealthLevelText, stealthCostText, stealthStatText,
                     stealthLevel, stealthBaseCost, stealthStatGain, PlayerStats.Instance.stealth, "은신도");
    }

    void UpdateStatUI(Button upgradeBtn, Button downgradeBtn,
                      Text levelTxt, Text costTxt, Text statTxt,
                      int level, int baseCost, int statGain, int statValue, string statName)
    {
        bool maxed  = level >= maxLevel;
        bool minned = level <= 1;
        int  upgradeCost   = GetCost(level, baseCost);
        int  downgradeCost = GetCost(level - 1, baseCost); // 환불 금액
        int  coins = PlayerStats.Instance != null ? PlayerStats.Instance.coins : 0;

        if (levelTxt != null)
            levelTxt.text = maxed ? "Lv. MAX" : $"Lv. {level} / {maxLevel}";

        if (costTxt != null)
        {
            if (maxed)
                costTxt.text = $"최대 레벨 | 환불 {downgradeCost}코인";
            else
                costTxt.text = $"업그레이드 {upgradeCost}코인 | 환불 {downgradeCost}코인";
        }

        if (statTxt != null)
            statTxt.text = $"{statName}: {statValue} (+{statGain}/레벨)";

        if (upgradeBtn != null)
            upgradeBtn.interactable = !maxed && coins >= upgradeCost;

        if (downgradeBtn != null)
            downgradeBtn.interactable = !minned;
    }
}
