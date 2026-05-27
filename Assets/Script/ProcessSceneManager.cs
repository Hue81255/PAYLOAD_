using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProcessSceneManager : MonoBehaviour
{
    public static ProcessSceneManager Instance;

    [Tooltip("돌아갈 메인 씬 이름. Build Settings에 등록돼 있어야 한다.")]
    public string mainSceneName = "New main";

    [Header("지역 이름 표시 (선택)")]
    public TMP_Text regionNameText;

    [Header("지역 현황 Wavecircle (선택) — 인스펙터에서 연결")]
    public Wavecircle infectionCircle;   // 전염도 충족률
    public Wavecircle penRateCircle;     // 침투율(복잡도) 충족률
    public Wavecircle detectionCircle;   // 은신도 충족률

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (TraitTree.TraitTreeManager.Instance != null)
            TraitTree.TraitTreeManager.Instance.OnTreeChanged -= UpdateRegionDisplay;
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
            SaveManager.Instance.Load();

        TraitTree.TraitTreeManager.Instance?.SetCurrentRegion(GameFlowData.SelectedRegionId);

        // 트레이트 노드 언락 시 수치 즉시 갱신
        if (TraitTree.TraitTreeManager.Instance != null)
            TraitTree.TraitTreeManager.Instance.OnTreeChanged += UpdateRegionDisplay;

        UpdateRegionDisplay();
    }

    // ── 지역 현황 수치 갱신 ──────────────────────────────────────

    void UpdateRegionDisplay()
    {
        string regionId = GameFlowData.SelectedRegionId;

        // RegionDataLoader가 없는 씬이면 JSON에서 직접 로드
        RegionData region = RegionDataLoader.Instance?.GetRegionById(regionId);
        if (region == null)
        {
            TextAsset json = Resources.Load<TextAsset>("Data");
            if (json != null)
            {
                var list = JsonUtility.FromJson<RegionDataList>(json.text);
                region = list?.regions?.Find(r => r.id == regionId);
            }
        }

        if (region == null)
        {
            Debug.LogWarning($"[ProcessSceneManager] 지역 데이터 없음: '{regionId}'");
            return;
        }

        if (regionNameText != null)
            regionNameText.text = region.name;

        // 플레이어 스탯 + 이 지역의 트레이트 보너스
        int inf = (PlayerStats.Instance?.inf     ?? 0)
                + (TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(regionId, TraitTree.TraitCategory.Inf)     ?? 0);
        int comp = (PlayerStats.Instance?.comp   ?? 0)
                + (TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(regionId, TraitTree.TraitCategory.Comp)    ?? 0);
        int stealth = (PlayerStats.Instance?.stealth ?? 0)
                + (TraitTree.TraitTreeManager.Instance?.GetRegionStatBonus(regionId, TraitTree.TraitCategory.Stealth) ?? 0);

        // 지역 방어 스탯 (0 방지)
        int defInf     = Mathf.Max(1, region.minStats?.inf     ?? 1);
        int defComp    = Mathf.Max(1, region.minStats?.comp    ?? 1);
        int defStealth = Mathf.Max(1, region.minStats?.stealth ?? 1);

        // 충족률 0~100 계산
        float infRate     = Mathf.Clamp(inf     * 100f / defInf,     0f, 100f);
        float penRate     = Mathf.Clamp(comp    * 100f / defComp,    0f, 100f);
        float detRate     = Mathf.Clamp(stealth * 100f / defStealth, 0f, 100f);

        if (infectionCircle != null) infectionCircle.SetPercent(infRate);
        if (penRateCircle   != null) penRateCircle.SetPercent(penRate);
        if (detectionCircle != null) detectionCircle.SetPercent(detRate);
    }

    // ── 씬 전환 ─────────────────────────────────────────────────

    public void BackToGame()
    {
        SaveManager.Instance?.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
    }

    public void BackToSelect()
    {
        SaveManager.Instance?.DeleteSave();
        GameFlowData.IsNewGame = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("select");
    }
}
