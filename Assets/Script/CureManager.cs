using UnityEngine;

public class CureManager : MonoBehaviour
{
    public static CureManager Instance;

    [Header("발각 진행도")]
    [Range(0f, 100f)]
    public float cureProgress = 0f;
    public float baseCureSpeed = 0.3f;

    [Header("스탯")]
    public float stealth = 0f;

    [Header("낮/밤 배율")]
    public float daySpeedMultiplier = 1.5f;
    public float nightSpeedMultiplier = 0.5f;

    [Header("이벤트 발동 여부")]
    private bool phase1Triggered = false;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private bool gameOverCalled = false; // 중복 호출 방지

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;
        if (gameOverCalled) return; // 게임오버 후 중복 호출 방지

        float timeMultiplier = 1f;
        if (TimeManager.instance != null)
        {
            timeMultiplier = TimeManager.instance.isNight
                ? nightSpeedMultiplier
                : daySpeedMultiplier;
        }

        float speed = (baseCureSpeed - (stealth * 0.005f)) * timeMultiplier;
        speed = Mathf.Max(0.05f, speed);

        cureProgress += speed * Time.deltaTime;
        cureProgress = Mathf.Clamp(cureProgress, 0f, 100f);

        TriggerPhaseEvents();

        if (cureProgress >= 100f && !gameOverCalled)
        {
            gameOverCalled = true;
            Debug.Log("발각도 100%! GameOver 호출!");
            GameManager.Instance.GameOver();
        }
    }

    void TriggerPhaseEvents()
    {
        if (!phase1Triggered && cureProgress >= 30f)
        {
            phase1Triggered = true;
            Debug.Log("⚠️ 경고: 백신 프로토타입 개발 시작!");
        }
        if (!phase2Triggered && cureProgress >= 60f)
        {
            phase2Triggered = true;
            Debug.Log("⚠️ 경고: 방화벽 구축 시작!");
        }
        if (!phase3Triggered && cureProgress >= 90f)
        {
            phase3Triggered = true;
            Debug.Log("⚠️ 위험: 포렌식 감시 시작!");
            stealth -= 10f;
        }
    }

    public void OnRegionInfected()
    {
        baseCureSpeed += 0.05f;
    }

    public void UpdateStealth(float newStealth)
    {
        stealth = newStealth;
    }

    public void ResetCure()
    {
        cureProgress = 0f;
        baseCureSpeed = 0.3f;
        stealth = 0f;
        gameOverCalled = false;
        phase1Triggered = false;
        phase2Triggered = false;
        phase3Triggered = false;
    }
}
