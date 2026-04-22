using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DefenseMin
{
    public int infection;
    public int complexity;
    public int stealth;
}

[System.Serializable]
public class RegionInfo
{
    public string name;
    public string type;
    public int population;
    public DefenseMin defenseMin;
    public float rewardMultiplier;
    public List<string> adjacent;
}

[System.Serializable]
public class RegionDatabase
{
    public List<RegionInfo> regions;
}

public class RegionDataLoader : MonoBehaviour
{
    public static RegionDataLoader Instance;
    public RegionDatabase database;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadData();
    }

    void LoadData()
    {
        TextAsset json = Resources.Load<TextAsset>("Data");
        if (json != null)
        {
            database = JsonUtility.FromJson<RegionDatabase>(json.text);
            Debug.Log($"✅ 지역 데이터 로드 완료: {database.regions.Count}개 구역");
        }
        else
        {
            Debug.LogError("❌ Data.json 파일을 찾을 수 없어요!");
        }
    }

    public RegionInfo GetRegion(string name)
    {
        return database.regions.Find(r => r.name == name);
    }
}
