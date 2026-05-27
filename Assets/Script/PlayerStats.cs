using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("기본 스탯")]
    public int inf = 10;        // 전염도
    public int comp = 10;       // 복잡도
    public int stealth = 10;    // 은신도

    [Header("코인")]
    public int coins = 100;

    [Header("패시브 코인 수입")]
    public float passiveIntervalMin = 10f;
    public float passiveIntervalMax = 15f;
    public int   passiveAmountMin   = 3;
    public int   passiveAmountMax   = 5;

    float _passiveTimer    = 0f;
    float _nextPassiveTime = 0f;

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
        _nextPassiveTime = Random.Range(passiveIntervalMin, passiveIntervalMax);
    }

    void Update()
    {
        // 메인씬: 게임 진행 중 + 첫 감염 발생 이후부터 수입 시작
        // Process씬: GameManager가 없으므로 IsNewGame=false면 진행 중으로 간주
        bool gameActive = GameManager.Instance != null
            ? (GameManager.Instance.isGameStarted && GameManager.Instance.infectedRegions > 0)
            : !GameFlowData.IsNewGame;

        if (!gameActive) return;

        _passiveTimer += Time.deltaTime;
        if (_passiveTimer < _nextPassiveTime) return;

        _passiveTimer    = 0f;
        _nextPassiveTime = Random.Range(passiveIntervalMin, passiveIntervalMax);
        int amount = Random.Range(passiveAmountMin, passiveAmountMax + 1);
        AddCoins(amount);
    }

    // 코인 추가/차감
    public void AddCoins(int amount)
    {
        coins += amount;
        coins = Mathf.Max(0, coins); // 0 이하로 안 내려가게
        Debug.Log($"💰 코인: {coins} ({(amount >= 0 ? "+" : "")}{amount})");
    }

    // 스탯 업그레이드
    public void UpgradeInf(int amount)
    {
        inf += amount;
        // InfectionEngine 스탯도 같이 업데이트
        if (InfectionEngine.Instance != null)
            InfectionEngine.Instance.playerInf = inf;
        Debug.Log($"전염도 업그레이드: {inf}");
    }

    public void UpgradeComp(int amount)
    {
        comp += amount;
        if (InfectionEngine.Instance != null)
            InfectionEngine.Instance.playerComp = comp;
        Debug.Log($"복잡도 업그레이드: {comp}");
    }

    public void UpgradeStealth(int amount)
    {
        stealth += amount;
        if (InfectionEngine.Instance != null)
            InfectionEngine.Instance.playerStealth = stealth;
        // CureManager 은신도 연동
        if (CureManager.Instance != null)
            CureManager.Instance.UpdateStealth(stealth);
        Debug.Log($"은신도 업그레이드: {stealth}");
    }

    public void ResetStats()
    {
        inf = 10;
        comp = 10;
        stealth = 10;
        coins = 100;
    }

    public void ApplyMalware(MalwareDefinition def)
    {
        ResetStats();
        inf     += def.infBonus;
        comp    += def.compBonus;
        stealth += def.stealthBonus;
        coins   = Mathf.Max(0, coins + def.coinBonus);

        if (InfectionEngine.Instance != null)
        {
            InfectionEngine.Instance.playerInf     = inf;
            InfectionEngine.Instance.playerComp    = comp;
            InfectionEngine.Instance.playerStealth = stealth;
        }
    }
}
