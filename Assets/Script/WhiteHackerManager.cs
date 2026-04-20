using UnityEngine;

public class WhiteHackerManager : MonoBehaviour
{
    public float cureProgress = 0f; // 0 ~ 100%
    public float cureSpeed = 0.5f;   // 기본 치료 속도
    public bool isCuring = false;

    void Update()
    {
        // 전체 감염도가 특정 임계점을 넘으면 치료 시작 (예: 10% 이상 감염 시)
        if (EvolutionManager.Instance.infectionLevel > 2 || isCuring)
        {
            isCuring = true;
            ExecuteCure();
        }
    }

    void ExecuteCure()
    {
        // 복잡도(Complexity) 스탯이 높을수록 치료 속도가 느려짐
        float complexityModifier = 1.0f / (EvolutionManager.Instance.complexityLevel * 0.5f + 1);
        cureProgress += cureSpeed * complexityModifier * Time.deltaTime;

        if (cureProgress >= 100f)
        {
            Debug.Log("화이트해커가 바이러스를 완전히 박멸했습니다. 게임 오버!");
            // 게임 오버 이벤트 호출 로직 추가 가능
        }

        // 특정 수치 도달 시 백도어 작동 준비 알림 (예: 50%)
        if (cureProgress >= 50f)
        {
            GlobalEventManager.CallBackdoorActive();
        }
    }
}