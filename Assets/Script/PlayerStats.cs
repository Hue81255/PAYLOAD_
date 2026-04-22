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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
}
