using System;
using System.Collections.Generic;

// 각 구역의 스탯 수치
[Serializable]
public class DefenseStats
{
    public int inf;      // 전염도 (Infection)
    public int comp;     // 복잡도 (Complexity)
    public int stealth;  // 은신도 (Stealth)
}

// 구역 하나당 상세 데이터
[Serializable]
public class RegionData
{
    public string id;
    public string name;
    public string type;
    public DefenseStats minStats;
    public int reward;
    public bool isBoss;
    public bool isInfected = false;

}

// JSON 파일 전체를 읽어오기 위한 리스트 래퍼
[Serializable]
public class RegionDataList
{
    public List<RegionData> regions;
}