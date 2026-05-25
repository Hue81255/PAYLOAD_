using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 세이브/로드를 담당하는 싱글톤 매니저.
/// 저장 경로: Application.persistentDataPath/payload_save.json
/// - 자동 저장: 해킹 성공, 업그레이드 구매
/// - 세이브 삭제: 게임 오버, 게임 클리어 (패배/승리 후 새 게임 강제)
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

    // 자동 저장: 게임이 실제로 진행 중일 때만
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
        var data = new SaveData();
        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 악성코드
        data.malwareType = MalwareSelectionManager.Instance != null
            ? (int)MalwareSelectionManager.Instance.selectedType : 0;

        // PlayerStats
        if (PlayerStats.Instance != null)
        {
            data.inf     = PlayerStats.Instance.inf;
            data.comp    = PlayerStats.Instance.comp;
            data.stealth = PlayerStats.Instance.stealth;
            data.coins   = PlayerStats.Instance.coins;
        }

        // EvolutionManager
        if (EvolutionManager.Instance != null)
        {
            data.infLevel     = EvolutionManager.Instance.infLevel;
            data.compLevel    = EvolutionManager.Instance.compLevel;
            data.stealthLevel = EvolutionManager.Instance.stealthLevel;
        }

        // GameManager (Process씬에서는 null일 수 있음)
        data.infectedRegions = GameManager.Instance != null ? GameManager.Instance.infectedRegions : 0;

        // CureManager
        CureManager.Instance?.FillSaveData(data);

        // WhiteHackerManager
        WhiteHackerManager.Instance?.FillSaveData(data);

        // 구역 감염 상태
        data.regions = new List<RegionSaveData>();
        if (InfectionEngine.Instance != null)
        {
            foreach (var r in InfectionEngine.Instance.regions)
                data.regions.Add(new RegionSaveData { regionId = r.id, isInfected = r.isInfected });
        }

        // TraitTree 언락 노드
        // Process씬에서는 TraitTreeManager가 살아있으므로 직접 읽고,
        // 메인씬에서 저장할 때는 기존 파일의 목록을 그대로 보존한다.
        if (TraitTree.TraitTreeManager.Instance != null)
            data.unlockedTraitNodes = TraitTree.TraitTreeManager.Instance.GetUnlockedNames();
        else if (HasSave(slot))
        {
            try
            {
                var prev = JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
                data.unlockedTraitNodes = prev?.unlockedTraitNodes ?? new List<string>();
            }
            catch { data.unlockedTraitNodes = new List<string>(); }
        }
        else
            data.unlockedTraitNodes = new List<string>();

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

        // ── 9. 악성코드 패시브 복원 (UI 패널 없이 재개) ──────────
        MalwareSelectionManager.Instance?.RestoreFromSave(data.malwareType);

        // ── 10. TraitTree 언락 상태 복원 (Process씬에서만 동작) ──
        if (TraitTree.TraitTreeManager.Instance != null && data.unlockedTraitNodes != null)
            TraitTree.TraitTreeManager.Instance.RestoreFromNames(data.unlockedTraitNodes);

        // ── 11. 게임 상태 전환 ───────────────────────────────────
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
