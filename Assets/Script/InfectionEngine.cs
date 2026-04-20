using UnityEngine;

public class InfectionEngine : MonoBehaviour
{
    public int playerInf = 10;
    public int playerComp = 10;
    public int playerStealth = 10;

    // 여기서 RegionData를 인자로 받을 때, 
    // 위에서 정의한 RegionData 클래스를 참조
    public void AttemptHack(RegionData target)
    {
        if (target == null) return;

        if (playerInf >= target.minStats.inf &&
            playerComp >= target.minStats.comp &&
            playerStealth >= target.minStats.stealth)
        {

            GlobalEventManager.CallHackSuccess(target.id, target.reward);
            Debug.Log($"{target.name} 해킹 성공!");
        }
        else
        {
            Debug.Log($"{target.name} 보안이 너무 강력합니다.");
        }
    }
}