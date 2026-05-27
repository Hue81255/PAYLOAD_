using System;
using System.Collections.Generic;

[Serializable]
public class RegionSaveData
{
    public string regionId;
    public bool   isInfected;
}

[Serializable]
public class SaveData
{
    // 메타
    public string savedAt;

    // 악성코드 선택 (MalwareType enum → int)
    public int malwareType;

    // PlayerStats
    public int inf;
    public int comp;
    public int stealth;
    public int coins;

    // EvolutionManager 업그레이드 레벨
    public int infLevel;
    public int compLevel;
    public int stealthLevel;

    // GameManager
    public int infectedRegions;

    // CureManager
    public float cureProgress;
    public float baseCureSpeed;
    public float cureManagerStealth;
    public bool  cureStarted;
    public bool  warningShown;
    public bool  phase1Triggered;
    public bool  phase2Triggered;
    public bool  phase3Triggered;
    public float cureSuppressionTimer;

    // WhiteHackerManager
    public int    hackerState;       // HackerState enum → int
    public float  hackerCureProgress;
    public bool   isCuring;
    public string targetRegion;
    public float  regionCureTimer;
    public float  scanTimer;

    // 구역 감염 상태
    public List<RegionSaveData> regions;

    // 지역별 TraitTree 언락 노드
    public List<RegionTraitSaveData> regionTraitNodes = new List<RegionTraitSaveData>();
}

[Serializable]
public class RegionTraitSaveData
{
    public string       regionId;
    public List<string> unlockedNodes = new List<string>();
    // 보너스 수치를 저장해두면 ScriptableObject 없는 씬(Main)에서도 복원 가능
    public int infBonus;
    public int compBonus;
    public int stealthBonus;
}
