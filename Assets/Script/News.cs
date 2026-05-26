using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ── 게임 진행 단계 ──
public enum GameTier { Early, Mid, Late }

// ── 뉴스 한 건의 데이터 ──
[System.Serializable]
public class NewsItem
{
    public string id;
    public string tier;       // flavor 전용: early/mid/late
    public string condition;  // info 전용: 조건 키
    public string type;       // news/internet/analysis
    public string headline;
    public string body;
    public string hint;       // info 전용: 플레이어 힌트
    public string malware;    // malwareNews 전용: 악성코드 이름
}

[System.Serializable]
public class NewsDatabase
{
    public List<NewsItem> flavorNews;
    public List<NewsItem> infoNews;
    public List<NewsItem> malwareNews;
}

public class News : MonoBehaviour
{
    public static News Instance;

    [Header("뉴스 데이터 (Resources/News.json)")]
    public string jsonFileName = "News";

    [Header("플레이버 뉴스 등장 간격(초)")]
    public float flavorInterval = 25f;

    [Header("조건 체크 간격(초)")]
    public float conditionCheckInterval = 3f;

    // 뉴스 발행 이벤트 — NewsTickerUI 등이 구독
    public event Action<NewsItem> OnNewsPublished;

    private NewsDatabase db;
    private float flavorTimer      = 0f;
    private float conditionTimer   = 0f;
    private bool  malwareIntroDone = false;

    private HashSet<string> firedInfoNews = new HashSet<string>();
    private HashSet<string> usedFlavor    = new HashSet<string>();

    // ── 생명주기 ──────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        LoadNews();
    }

    void Update()
    {
        if (!IsGameRunning()) return;

        // 악성코드 인트로 뉴스 (최초 1회)
        if (!malwareIntroDone)
        {
            malwareIntroDone = true;
            PublishMalwareIntro(GetMalwareName());
        }

        flavorTimer    += Time.deltaTime;
        conditionTimer += Time.deltaTime;

        if (flavorTimer >= flavorInterval)
        {
            flavorTimer = 0f;
            PublishRandomFlavor();
        }

        if (conditionTimer >= conditionCheckInterval)
        {
            conditionTimer = 0f;
            CheckConditions(BuildCurrentState());
        }
    }

    static bool IsGameRunning() =>
        GameManager.Instance != null &&
        GameManager.Instance.isGameStarted &&
        !GameManager.Instance.isGameOver &&
        !GameManager.Instance.isGameClear;

    // ── JSON 로드 ─────────────────────────────────────────────

    void LoadNews()
    {
        TextAsset asset = Resources.Load<TextAsset>(jsonFileName);
        if (asset == null)
        {
            Debug.LogError($"[NewsManager] {jsonFileName}.json 을 Resources 에서 찾을 수 없습니다.");
            return;
        }
        db = JsonUtility.FromJson<NewsDatabase>(asset.text);
        Debug.Log($"[NewsManager] 뉴스 로드 완료 — 플레이버 {db.flavorNews?.Count}건 / 정보 {db.infoNews?.Count}건 / 악성코드 {db.malwareNews?.Count}건");
    }

    // ── 게임 상태 스냅샷 생성 ──────────────────────────────────

    NewsGameState BuildCurrentState()
    {
        int   captured      = GameManager.Instance?.infectedRegions ?? 0;
        int   total         = GameManager.Instance?.totalRegions    ?? 9;
        float capturedRatio = total > 0 ? (float)captured / total : 0f;

        float cureProgress  = CureManager.Instance?.cureProgress   ?? 0f;
        bool  cureStarted   = CureManager.Instance?.IsCureStarted ?? false;

        return new NewsGameState
        {
            malwareName        = GetMalwareName(),
            infection          = PlayerStats.Instance?.inf     ?? 0,
            stealth            = PlayerStats.Instance?.stealth ?? 0,
            complexity         = PlayerStats.Instance?.comp    ?? 0,
            coins              = PlayerStats.Instance?.coins   ?? 0,
            capturedCount      = captured,
            capturedRatio      = capturedRatio,
            maxRegionInfection = capturedRatio,           // 지역별 개별 감염률 미구현 — 전체 비율로 대체
            anyBossUnlocked    = IsBossAccessible(),
            treatmentActive    = cureStarted,
            treatmentProgress  = cureProgress,
            detectedCount      = cureStarted ? Mathf.FloorToInt(cureProgress / 30f) + 1 : 0
        };
    }

    // 보스 지역 접근 가능 여부 (isBoss 구역이 인접 감염 구역 옆에 있을 때)
    static bool IsBossAccessible()
    {
        if (InfectionEngine.Instance == null || RegionAdjacencyManager.Instance == null) return false;
        foreach (var r in InfectionEngine.Instance.regions)
        {
            if (!r.isBoss || r.isInfected) continue;
            var adj = RegionAdjacencyManager.Instance.GetAdjacentIds(r.id);
            foreach (var id in adj)
            {
                var neighbor = InfectionEngine.Instance.regions.Find(x => x.id == id);
                if (neighbor != null && neighbor.isInfected) return true;
            }
        }
        return false;
    }

    // MalwareType 열거형 → JSON malware 필드 이름 매핑
    static string GetMalwareName()
    {
        if (MalwareSelectionManager.Instance == null) return "";
        switch (MalwareSelectionManager.Instance.selectedType)
        {
            case MalwareType.Ransomware:  return "랜섬웨어";
            case MalwareType.Spyware:     return "스파이웨어";
            case MalwareType.Worm:        return "웜";
            case MalwareType.Trojan:      return "스틸러";      // 가장 유사한 매핑
            case MalwareType.Botnet:      return "크립토마이너"; // 가장 유사한 매핑
            default:                      return MalwareSelectionManager.Instance.selectedType.ToString();
        }
    }

    // ── 악성코드 인트로 뉴스 ──────────────────────────────────

    public void PublishMalwareIntro(string malwareName)
    {
        if (db?.malwareNews == null) return;
        var news = db.malwareNews.Find(n => n.malware == malwareName);
        if (news != null)
        {
            OnNewsPublished?.Invoke(news);
            Debug.Log($"[뉴스/악성코드] {news.headline}");
        }
    }

    // ── 플레이버 뉴스 ────────────────────────────────────────

    void PublishRandomFlavor()
    {
        if (db?.flavorNews == null) return;

        float capturedRatio = GameManager.Instance != null
            ? (float)GameManager.Instance.infectedRegions / GameManager.Instance.totalRegions : 0f;

        string tierStr = GetCurrentTier(capturedRatio).ToString().ToLower();

        var candidates = db.flavorNews
            .Where(n => n.tier == tierStr && !usedFlavor.Contains(n.id))
            .ToList();

        if (candidates.Count == 0)
        {
            // 전부 소진 시 해당 티어 초기화 후 재사용
            foreach (var n in db.flavorNews.Where(n => n.tier == tierStr))
                usedFlavor.Remove(n.id);
            candidates = db.flavorNews.Where(n => n.tier == tierStr).ToList();
        }
        if (candidates.Count == 0) return;

        var picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        usedFlavor.Add(picked.id);
        OnNewsPublished?.Invoke(picked);
        Debug.Log($"[뉴스/플레이버] {picked.headline}");
    }

    static GameTier GetCurrentTier(float capturedRatio)
    {
        if (capturedRatio < 0.33f) return GameTier.Early;
        if (capturedRatio < 0.66f) return GameTier.Mid;
        return GameTier.Late;
    }

    // ── 조건 뉴스 체크 ───────────────────────────────────────

    public void CheckConditions(NewsGameState state)
    {
        if (db == null) return;

        if (db.infoNews != null)
        {
            foreach (var news in db.infoNews)
            {
                if (firedInfoNews.Contains(news.id)) continue;
                if (EvaluateCondition(news.condition, state))
                {
                    firedInfoNews.Add(news.id);
                    OnNewsPublished?.Invoke(news);
                    Debug.Log($"[뉴스/정보] {news.headline}");
                }
            }
        }

        if (db.malwareNews != null)
        {
            foreach (var news in db.malwareNews)
            {
                if (firedInfoNews.Contains(news.id)) continue;
                if (EvaluateMalwareCondition(news.condition, state))
                {
                    firedInfoNews.Add(news.id);
                    OnNewsPublished?.Invoke(news);
                    Debug.Log($"[뉴스/악성코드조건] {news.headline}");
                }
            }
        }
    }

    // ── 조건 평가 ────────────────────────────────────────────

    bool EvaluateCondition(string condition, NewsGameState s)
    {
        switch (condition)
        {
            case "STEALTH_LOW":              return s.stealth <= 3;
            case "COMPLEXITY_LOW":           return s.complexity <= 3;
            case "INFECTION_LOW":            return s.infection <= 3;
            case "REGION_INFECTION_40":      return s.maxRegionInfection >= 0.40f;
            case "BOSS_UNLOCKED":            return s.anyBossUnlocked;
            case "TREATMENT_STARTED":        return s.treatmentActive;
            case "TREATMENT_HALF":           return s.treatmentActive && s.treatmentProgress >= 50f;
            case "TREATMENT_CRITICAL":       return s.treatmentActive && s.treatmentProgress >= 80f;
            case "COIN_HIGH":                return s.coins >= 300;
            case "DETECTED_ONCE":            return s.detectedCount >= 1;
            case "DETECTION_HIGH":           return s.detectedCount >= 1 || (s.treatmentActive && s.treatmentProgress >= 30f);
            case "FIRST_REGION_CAPTURED":    return s.capturedCount >= 1;
            case "HALF_CAPTURED":
            case "REGIONS_HALF_CAPTURED":    return s.capturedRatio >= 0.5f && s.capturedRatio < 0.8f;
            case "INFECTION_STAT_HIGH":      return s.infection >= 8;
            case "REGIONS_MOSTLY_CAPTURED":  return s.capturedRatio >= 0.8f && s.capturedRatio < 1f;
            default:
                Debug.LogWarning($"[NewsManager] 알 수 없는 조건: {condition}");
                return false;
        }
    }

    bool EvaluateMalwareCondition(string condition, NewsGameState s)
    {
        bool started = s.capturedCount > 0 || s.treatmentActive;
        if (!started) return false;

        switch (condition)
        {
            case "MALWARE_RANSOMWARE":  return s.malwareName == "랜섬웨어";
            case "MALWARE_SPYWARE":     return s.malwareName == "스파이웨어";
            case "MALWARE_WORM":        return s.malwareName == "웜";
            case "MALWARE_STEALER":     return s.malwareName == "스틸러";
            case "MALWARE_CRYPTOMINER": return s.malwareName == "크립토마이너";
            default:
                Debug.LogWarning($"[NewsManager] 알 수 없는 악성코드 조건: {condition}");
                return false;
        }
    }
}

// ── 조건 판단용 게임 상태 구조체 ──────────────────────────────

public struct NewsGameState
{
    public string malwareName;
    public int   infection;
    public int   stealth;
    public int   complexity;
    public int   coins;
    public float maxRegionInfection;
    public bool  anyBossUnlocked;
    public bool  treatmentActive;
    public float treatmentProgress;
    public int   detectedCount;
    public int   capturedCount;
    public float capturedRatio;
}
