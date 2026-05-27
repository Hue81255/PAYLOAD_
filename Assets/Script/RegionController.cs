using TMPro;
using UnityEngine;

public class RegionController : MonoBehaviour
{
    public string regionId;
    public RegionData data;

    // 실시간으로 변동되는 체감 스탯
    public float currentInf;
    public float currentStealth;

    [Header("약화 표시 UI (선택)")]
    [Tooltip("구역 오브젝트 위에 표시할 약화 텍스트. 없으면 무시됩니다.")]
    public TMP_Text weakenedLabel;

    [Header("구역 색상")]
    [Tooltip("감염됐을 때 색상 (기본: 빨간색)")]
    public Color infectedColor  = new Color(0.9f, 0.15f, 0.1f);
    [Tooltip("방어력 약화 시 색상 (기본: 주황색)")]
    public Color weakenedColor  = new Color(1f, 0.45f, 0f);

    private Color    originalColor;
    private Renderer regionRenderer;
    private bool     _lastInfectedState = false;

    void Awake()
    {
        regionRenderer = GetComponent<Renderer>();
        if (regionRenderer != null)
            originalColor = regionRenderer.material.color;
    }

    void Start()
    {
        // RegionDataLoader가 JSON에서 로드한 객체와 참조를 공유해야 InfectionEngine과 일치한다
        if (!string.IsNullOrEmpty(regionId) && RegionDataLoader.Instance != null)
        {
            var loaded = RegionDataLoader.Instance.GetRegionById(regionId);
            if (loaded != null) data = loaded;
        }
    }

    void OnEnable()
    {
        GlobalEventManager.OnHackSuccess    += OnHackSuccess;
        GlobalEventManager.OnTimeChanged    += UpdateStatsByTime;
        GlobalEventManager.OnDefenseChanged += OnDefenseChanged;
    }

    void OnDisable()
    {
        GlobalEventManager.OnHackSuccess    -= OnHackSuccess;
        GlobalEventManager.OnTimeChanged    -= UpdateStatsByTime;
        GlobalEventManager.OnDefenseChanged -= OnDefenseChanged;
    }

    void Update()
    {
        // 감염 상태가 바뀌었을 때만 색상 갱신 (로드 복원 포함)
        if (data != null && data.isInfected != _lastInfectedState)
        {
            _lastInfectedState = data.isInfected;
            RefreshColor();
        }
    }

    void OnHackSuccess(string regionId, int _)
    {
        if (data == null || data.id != regionId) return;
        _lastInfectedState = true;
        RefreshColor();
    }

    void RefreshColor()
    {
        if (regionRenderer == null || data == null) return;
        if (data.isInfected)
            regionRenderer.material.color = infectedColor;
        else
            regionRenderer.material.color = originalColor;
    }

    // ── 낮/밤 스탯 변동 ──────────────────────────────────────────
    void UpdateStatsByTime(float time, bool isNight)
    {
        if (data == null) return;

        if (data.type == "Business")
        {
            currentInf     = isNight ? data.minStats.inf * 1.5f    : data.minStats.inf;
            currentStealth = isNight ? data.minStats.stealth * 0.8f : data.minStats.stealth;
        }
        else if (data.type == "Residential")
        {
            currentInf = isNight ? data.minStats.inf * 0.7f : data.minStats.inf;
        }
    }

    // ── 인접 감염 약화 피드백 ─────────────────────────────────────
    void OnDefenseChanged(string changedId, int reduction)
    {
        if (data == null || data.id != changedId) return;

        bool isWeakened = reduction > 0;

        // 텍스트 라벨 갱신
        if (weakenedLabel != null)
        {
            weakenedLabel.gameObject.SetActive(isWeakened);
            if (isWeakened)
                weakenedLabel.text = $"방어 -{reduction}";
        }

        // 색상 변경
        if (regionRenderer != null)
            regionRenderer.material.color = isWeakened ? weakenedColor : originalColor;
    }
}
