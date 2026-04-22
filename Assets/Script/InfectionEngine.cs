using UnityEngine;
using System.Collections.Generic;

public class InfectionEngine : MonoBehaviour
{
    public static InfectionEngine Instance;

    public int playerInf = 10;
    public int playerComp = 10;
    public int playerStealth = 10;

    // 감염된 구역 목록
    public List<RegionData> regions = new List<RegionData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AttemptHack(RegionData target)
    {
        if (target == null) return;

        if (playerInf >= target.minStats.inf &&
            playerComp >= target.minStats.comp &&
            playerStealth >= target.minStats.stealth)
        {
            target.isInfected = true; // 감염 상태로 변경
            GlobalEventManager.CallHackSuccess(target.id, target.reward);
            Debug.Log($"{target.name} 해킹 성공!");
        }
        else
        {
            Debug.Log($"{target.name} 보안이 너무 강력합니다.");
        }
    }

    // 감염된 구역 중 랜덤으로 하나 반환
    public string GetRandomInfectedRegion()
    {
        List<string> infected = new List<string>();
        foreach (var region in regions)
        {
            if (region.isInfected)
                infected.Add(region.name);
        }
        if (infected.Count == 0) return "";
        return infected[Random.Range(0, infected.Count)];
    }

    // 구역 치료 (감염 해제)
    public void CureRegion(string regionName)
    {
        var region = regions.Find(r => r.name == regionName);
        if (region != null)
        {
            region.isInfected = false;
            Debug.Log($"[InfectionEngine] {regionName} 치료 완료");
        }
    }
}
