using System;
using System.Collections.Generic;

// �� ������ ���� ��ġ
[Serializable]
public class DefenseStats
{
    public int inf;      // ������ (Infection)
    public int comp;     // ���⵵ (Complexity)
    public int stealth;  // ���ŵ� (Stealth)
}

// 구역 하나의 기본 데이터
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

    // 런타임 전용 – JSON에 없음, 인접 구역 감염으로 인한 방어력 감소량
    [NonSerialized] public int defenseReduction = 0;
}

// JSON ���� ��ü�� �о���� ���� ����Ʈ ����
[Serializable]
public class RegionDataList
{
    public List<RegionData> regions;
}