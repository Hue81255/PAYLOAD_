using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 세이브/로드를 담당하는 싱글톤 매니저.
/// Process 씬처럼 일부 매니저가 없는 씬에서 저장할 때는
/// 이전 저장 파일의 값을 유지하여 데이터 오염을 방지한다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public static int CurrentSlot = 0;

    static string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"payload_save_{slot}.json");

    // ── 생명주기 ──────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()  => GlobalEventManager.OnHackSuccess += OnHackSuccess;
    void OnDisable() => GlobalEventManager.OnHackSuccess -= OnHackSuccess;

    void OnHackSuccess(string _, int __)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameStarted)
            Save();
    }

    // ── 공개 API ──────────────────────────────────────────────────

    public bool HasSave()           => HasSave(CurrentSlot);
    public bool HasSave(int slot)   => File.Exists(SlotPath(slot));

    // ─────────────────────────────────────────────────────────────
    // SAVE
    // ─────────────────────────────────────────────────────────────
    public void Save() => Save(CurrentSlot);

    public void Save(int slot)
    {
        // 이전 저장 파일을 먼저 읽어 null 매니저 필드의 보존값으로 활용
        SaveData prev = null;
        if (HasSave(slot))
        {
            try { prev = JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot))); }
            catch { }
        }

        var data = new SaveData();
        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // ── 악성코드 ──────────────────────────────────────────────
        data.malwareType = MalwareSelectionManager.Instance != null
            ? (int)MalwareSelectionManager.Instance.selectedType
            : (prev?.malwareType ?? 0);

        // ── PlayerStats ───────────────────────────────────────────
        if (PlayerStats.Instance != null)
        {
            data.inf     = PlayerStats.Instance.inf;
            data.comp    = PlayerStats.Instance.comp;
            data.stealth = PlayerStats.Instance.stealth;
            data.coins   = PlayerStats.Instance.coins;
        }
        else if (prev != null)
        {
            data.inf     = prev.inf;
            data.comp    = prev.comp;
            data.stealth = prev.stealth;
            data.coins   = prev.coins;
        }

        // ── EvolutionManager 업그레이드 레벨 ─────────────────────
        if (EvolutionManager.Instance != null)
        {
            data.infLevel     = EvolutionManager.Instance.infLevel;
            data.compLevel    = EvolutionManager.Instance.compLevel;
            data.stealthLevel = EvolutionManager.Instance.stealthLevel;
        }
        else if (prev != null)
        {
            data.infLevel     = prev.infLevel;
            data.compLevel    = prev.compLevel;
            data.stealthLevel = prev.stealthLevel;
        }

        // ── GameManager ───────────────────────────────────────────
        if (GameManager.Instance != null)
            data.infectedRegions = GameManager.Instance.infectedRegions;
        else
            data.infectedRegions = prev?.infectedRegions ?? 0;

        // ── CureManager ───────────────────────────────────────────
        if (CureManager.Instance != null)
            CureManager.Instance.FillSaveData(data);
        else if (prev != null)
        {
            data.cureProgress         = prev.cureProgress;
            data.baseCureSpeed        = prev.baseCureSpeed;
            data.cureManagerStealth   = prev.cureManagerStealth;
            data.cureStarted          = prev.cureStarted;
            data.warningShown         = prev.warningShown;
            data.phase1Triggered      = prev.phase1Triggered;
            data.phase2Triggered      = prev.phase2Triggered;
            data.phase3Triggered      = prev.phase3Triggered;
            data.cureSuppressionTimer = prev.cureSuppressionTimer;
        }

        // ── WhiteHackerManager ────────────────────────────────────
        if (WhiteHackerManager.Instance != null)
            WhiteHackerManager.Instance.FillSaveData(data);
        else if (prev != null)
        {
            data.hackerState        = prev.hackerState;
            data.isCuring           = prev.isCuring;
            data.targetRegion       = prev.targetRegion;
            data.regionCureTimer    = prev.regionCureTimer;
            data.scanTimer          = prev.scanTimer;
            data.hackerCureProgress = prev.hackerCureProgress;
        }

        // ── 구역 감염 상태 ────────────────────────────────────────
        // Process 씬처럼 InfectionEngine이 없으면 이전 저장값 유지
        if (InfectionEngine.Instance != null)
        {
            data.regions = new List<RegionSaveData>();
            foreach (var r in InfectionEngine.Instance.regions)
                data.regions.Add(new RegionSaveData { regionId = r.id, isInfected = r.isInfected });
        }
        else
            data.regions = prev?.regions ?? new List<RegionSaveData>();

        // ── 지역별 TraitTree 언락 노드 ────────────────────────────
        // Process 씬에서 업데이트된 트레이트 데이터 저장, 없으면 이전값 유지
        if (TraitTree.TraitTreeManager.Instance != null)
            data.regionTraitNodes = TraitTree.TraitTreeManager.Instance.GetAllSaveData();
        else
            data.regionTraitNodes = prev?.regionTraitNodes ?? new List<RegionTraitSaveData>();

        File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
        Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료 ({data.savedAt})");
    }

    // ─────────────────────────────────────────────────────────────
    // LOAD
    // ─────────────────────────────────────────────────────────────
    public bool Load() => Load(CurrentSlot);

    public bool Load(int slot)
    {
        if (!HasSave(slot))
        {
            Debug.Log($"[SaveManager] 슬롯 {slot} 저장 파일 없음.");
            return false;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 파일 파싱 실패: {e.Message}");
            return false;
        }

        // ── 1. 구역 감염 상태 복원 ──────────────────────────────
        if (InfectionEngine.Instance != null && data.regions != null)
        {
            foreach (var saved in data.regions)
            {
                var r = InfectionEngine.Instance.regions.Find(x => x.id == saved.regionId);
                if (r != null)
                {
                    r.isInfected      = saved.isInfected;
                    r.defenseReduction = 0;
                }
            }
        }

        // ── 2. 인접 방어력 재계산 ────────────────────────────────
        RegionAdjacencyManager.Instance?.RecalculateAll();

        // ── 3. PlayerStats 복원 ──────────────────────────────────
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.inf     = data.inf;
            PlayerStats.Instance.comp    = data.comp;
            PlayerStats.Instance.stealth = data.stealth;
            PlayerStats.Instance.coins   = data.coins;
        }

        // ── 4. InfectionEngine 스탯 동기화 ──────────────────────
        if (InfectionEngine.Instance != null)
        {
            InfectionEngine.Instance.playerInf     = data.inf;
            InfectionEngine.Instance.playerComp    = data.comp;
            InfectionEngine.Instance.playerStealth = data.stealth;
        }

        // ── 5. EvolutionManager 레벨 복원 ───────────────────────
        if (EvolutionManager.Instance != null)
        {
            EvolutionManager.Instance.infLevel     = data.infLevel;
            EvolutionManager.Instance.compLevel    = data.compLevel;
            EvolutionManager.Instance.stealthLevel = data.stealthLevel;
        }

        // ── 6. CureManager 상태 복원 ─────────────────────────────
        CureManager.Instance?.ApplyLoadData(data);

        // ── 7. WhiteHackerManager 상태 복원 ──────────────────────
        WhiteHackerManager.Instance?.ApplyLoadData(data);

        // ── 8. GameManager 카운트 복원 ───────────────────────────
        if (GameManager.Instance != null)
            GameManager.Instance.infectedRegions = data.infectedRegions;

        // ── 9. 악성코드 패시브 복원 ──────────────────────────────
        MalwareSelectionManager.Instance?.RestoreFromSave(data.malwareType);

        // ── 10. 지역별 TraitTree 언락 상태 복원 ──────────────────
        if (TraitTree.TraitTreeManager.Instance != null && data.regionTraitNodes != null)
            TraitTree.TraitTreeManager.Instance.RestoreFromSaveData(data.regionTraitNodes);

        // ── 11. 게임 상태 전환 (Main 씬에서만 GameManager 존재) ──
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameStarted = true;
            GameManager.Instance.isGameOver    = false;
            GameManager.Instance.isGameClear   = false;
            Time.timeScale = 1f;
            UIManager.Instance?.ResetUI();
            SpreadManager.Instance?.StartSpread();
        }

        Debug.Log($"[SaveManager] 슬롯 {slot} 로드 완료 (저장 시각: {data.savedAt})");
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────────────
    public void DeleteSave()           => DeleteSave(CurrentSlot);

    public void DeleteSave(int slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return;
        File.Delete(path);
        Debug.Log($"[SaveManager] 슬롯 {slot} 저장 파일 삭제.");
    }
}
