using UnityEngine;
using System.Collections.Generic;

public class RegionDataLoader : MonoBehaviour
{
    public static RegionDataLoader Instance;

    public List<RegionData> regions = new List<RegionData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        LoadData();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (InfectionEngine.Instance != null && regions.Count > 0)
        {
            // GameManager.InitGame()은 코루틴으로 1프레임 후 실행되므로
            // 이 Start()가 항상 먼저 실행됨 → regions를 항상 초기화
            InfectionEngine.Instance.regions = new List<RegionData>(regions);
            Debug.Log($"[RegionDataLoader] InfectionEngine에 {regions.Count}개 구역 데이터 연결 완료");
        }
    }

    void LoadData()
    {
        TextAsset json = Resources.Load<TextAsset>("Data");
        if (json == null)
        {
            Debug.LogError("[RegionDataLoader] Data.json 파일을 찾을 수 없습니다!");
            return;
        }

        var list = JsonUtility.FromJson<RegionDataList>(json.text);
        if (list?.regions != null)
        {
            regions = list.regions;
            Debug.Log($"[RegionDataLoader] {regions.Count}개 구역 로드 완료");
        }
        else
        {
            Debug.LogError("[RegionDataLoader] Data.json 파싱 실패 — 구조를 확인하세요.");
        }
    }

    public RegionData GetRegionById(string id)
    {
        return regions.Find(r => r.id == id);
    }

    public RegionData GetRegionByName(string name)
    {
        return regions.Find(r => r.name == name);
    }
}
