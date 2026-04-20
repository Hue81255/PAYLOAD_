using UnityEngine;

public class EvolutionManager : MonoBehaviour
{
    public static EvolutionManager Instance;

    [Header("Player Stats")]
    public int currentCoin = 0;
    public int infectionLevel = 1;
    public int stealthLevel = 1;
    public int complexityLevel = 1;

    [Header("Upgrade Costs")]
    public int upgradeCost = 500; // 기본 업그레이드 비용 (수정가능)

    void Awake() => Instance = this;

    void OnEnable()
    {
        // 해킹 성공 이벤트를 구독하여 코인 증가
        GlobalEventManager.OnHackSuccess += AddCoin;
    }

    void OnDisable()
    {
        GlobalEventManager.OnHackSuccess -= AddCoin;
    }

    void AddCoin(string id, int reward)
    {
        currentCoin += reward;
        Debug.Log($"코인 획득! 현재 코인: {currentCoin}");
    }

    // 브루트포스 루트: 전염성 강화
    public void UpgradeInfection()
    {
        if (currentCoin >= upgradeCost)
        {
            currentCoin -= upgradeCost;
            infectionLevel++;
            Debug.Log("브루트포스 진화! 전염성 스탯 상승");

            // InfectionEngine의 실제 스탯에 반영하는 로직을 나중에 추가
        }
    }

    // 트로이 목마 루트: 은신력 강화
    public void UpgradeStealth()
    {
        if (currentCoin >= upgradeCost)
        {
            currentCoin -= upgradeCost;
            stealthLevel++;
            Debug.Log("트로이 목마 진화! 은신력 스탯 상승");
        }
    }
}