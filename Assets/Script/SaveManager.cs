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

    static string SavePath =>
        Path.Combine(Application.persistentDataPath, "payload_save.json");

    // ── 생명주기 ──────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()  => GlobalEventManager.OnHackSuccess += OnHackSuccess;
    void OnDisable() => GlobalEventManager.OnHackSuccess -= OnHackSuccess;

    void OnHackSuccess(string _, int __) => Save();

    // ── 공개 API ──────────────────────────────────────────────────

    public bool HasSave() => File.Exists(SavePath);

    // ─────────────────────────────────────────────────────────────
    // SAVE
    // ─────────────────────────────────────────────────────────────
    public void Save()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;

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

        // GameManager
        data.infectedRegions = GameManager.Instance.infectedRegions;

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

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"[SaveManager] 저장 완료 ({data.savedAt})");
    }

    // ─────────────────────────────────────────────────────────────
    // LOAD
    // ─────────────────────────────────────────────────────────────
    public bool Load()
    {
        if (!HasSave())
        {
            Debug.Log("[SaveManager] 저장 파일 없음.");
            return false;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
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

        // ── 10. 게임 상태 전환 (리셋 없이) ──────────────────────
        GameManager.Instance?.LoadAndStartGame();

        Debug.Log($"[SaveManager] 로드 완료 (저장 시각: {data.savedAt})");
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────────────
    public void DeleteSave()
    {
        if (!HasSave()) return;
        File.Delete(SavePath);
        Debug.Log("[SaveManager] 저장 파일 삭제.");
    }
}
