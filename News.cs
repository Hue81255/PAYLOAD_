// =============================================
// NewsManager.cs
// 뉴스 이벤트 시스템 - Plague Inc. 스타일 뉴스 티커
//
// 두 종류의 뉴스를 관리:
//   1. 플레이버 뉴스 (flavorNews) - 게임 진행도(tier)에 따라 랜덤 등장
//   2. 정보 뉴스 (infoNews)      - 특정 조건 달성 시 1회 등장 (힌트 제공)
//
// 사용법:
//   1. NewsData.json을 Assets/Resources/ 에 넣기
//   2. 빈 게임오브젝트에 이 스크립트 붙이기
//   3. 뉴스 표시 UI(상단 NEWS 바)에 OnNewsPublished 이벤트 연결
// =============================================

using System;
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
    // headline 제거됨 - body만 사용
    public string body;
    // hint 제거됨
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
    [Header("뉴스 데이터 (Resources/NewsData.json)")]
    public string jsonFileName = "News";

    [Header("플레이버 뉴스 등장 간격(초)")]
    public float flavorInterval = 25f;

    // 뉴스가 발행될 때 UI가 구독하는 이벤트
    // body, type을 넘김
    public event Action<NewsItem> OnNewsPublished;

    private NewsDatabase db;
    private float timer = 0f;

    // 이미 등장한 정보 뉴스 (중복 방지)
    private HashSet<string> firedInfoNews = new HashSet<string>();
    // 이미 등장한 플레이버 뉴스 (가급적 중복 회피)
    private HashSet<string> usedFlavor = new HashSet<string>();

    void Start()
    {
        LoadNews();
    }

    // =============================================
    //  JSON 로드
    // =============================================
    private void LoadNews()
    {
        TextAsset asset = Resources.Load<TextAsset>(jsonFileName);
        if (asset == null)
        {
            Debug.LogError($"[NewsManager] {jsonFileName}.json을 Resources에서 찾을 수 없습니다.");
            return;
        }
        db = JsonUtility.FromJson<NewsDatabase>(asset.text);
        Debug.Log($"[NewsManager] 뉴스 로드 완료: 플레이버 {db.flavorNews.Count}건, " +
                  $"정보 {db.infoNews.Count}건, 악성코드별 {db.malwareNews.Count}건");
    }

    // =============================================
    //  게임 시작 시 선택한 악성코드 전용 뉴스 1건 발행
    //  GameManager.StartGame()에서 호출
    //  malwareName: 선택한 악성코드 이름 (예: "랜섬웨어")
    // =============================================
    public void PublishMalwareIntro(string malwareName)
    {
        if (db == null || db.malwareNews == null) return;

        var news = db.malwareNews.Find(n => n.malware == malwareName);
        if (news != null)
        {
            OnNewsPublished?.Invoke(news);
            Debug.Log($"[뉴스/악성코드] {news.body}");
        }
    }

    void Update()
    {
        // 플레이버 뉴스 타이머
        timer += Time.deltaTime;
        if (timer >= flavorInterval)
        {
            timer = 0f;
            PublishRandomFlavor();
        }
    }

    // =============================================
    //  현재 게임 진행 단계 판단
    //  (전체 지역 중 점령 비율로 tier 결정)
    // =============================================
    private GameTier GetCurrentTier(float capturedRatio)
    {
        if (capturedRatio < 0.33f) return GameTier.Early;
        if (capturedRatio < 0.66f) return GameTier.Mid;
        return GameTier.Late;
    }

    // =============================================
    //  플레이버 뉴스 랜덤 발행
    //  외부에서 capturedRatio를 세팅해두거나 인자로 받도록 확장 가능
    // =============================================
    public float currentCapturedRatio = 0f; // 외부(GameManager)에서 갱신

    private void PublishRandomFlavor()
    {
        if (db == null || db.flavorNews == null) return;

        GameTier tier = GetCurrentTier(currentCapturedRatio);
        string tierStr = tier.ToString().ToLower();

        // 현재 단계에 맞는 뉴스 중 아직 안 쓴 것 우선
        var candidates = db.flavorNews
            .Where(n => n.tier == tierStr && !usedFlavor.Contains(n.id))
            .ToList();

        // 다 썼으면 사용 기록 초기화 후 재사용
        if (candidates.Count == 0)
        {
            candidates = db.flavorNews.Where(n => n.tier == tierStr).ToList();
            foreach (var n in candidates) usedFlavor.Remove(n.id);
        }
        if (candidates.Count == 0) return;

        var picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        usedFlavor.Add(picked.id);
        OnNewsPublished?.Invoke(picked);
        Debug.Log($"[뉴스/플레이버] {picked.body}");
    }

    // =============================================
    //  정보 뉴스 조건 체크 (핵심)
    //  GameManager가 매 프레임 또는 상태 변경 시 호출
    //
    //  PlayerStats는 팀의 3스탯 버전(inf/comp/stealth/coins) 기준
    // =============================================
    public void CheckConditions(NewsGameState state)
    {
        if (db == null) return;

        // ── 정보 뉴스 체크 ──
        if (db.infoNews != null)
        {
            foreach (var news in db.infoNews)
            {
                if (firedInfoNews.Contains(news.id)) continue;

                if (EvaluateCondition(news.condition, state))
                {
                    firedInfoNews.Add(news.id);
                    OnNewsPublished?.Invoke(news);
                    Debug.Log($"[뉴스/정보] {news.body}");
                }
            }
        }

        // ── 악성코드 전용 뉴스 체크 ──
        // 선택한 악성코드와 일치하는 뉴스를 1회 등장
        if (db.malwareNews != null)
        {
            foreach (var news in db.malwareNews)
            {
                if (firedInfoNews.Contains(news.id)) continue;

                if (EvaluateMalwareCondition(news.condition, state))
                {
                    firedInfoNews.Add(news.id);
                    OnNewsPublished?.Invoke(news);
                    Debug.Log($"[뉴스/악성코드] {news.body}");
                }
            }
        }
    }

    // =============================================
    //  악성코드 전용 뉴스 조건 평가
    //  선택한 악성코드 이름과 비교
    // =============================================
    private bool EvaluateMalwareCondition(string condition, NewsGameState s)
    {
        // 악성코드 전용 뉴스는 게임 시작 후 잠깐 지난 뒤 등장하도록
        // 약간의 진행도(첫 감염 발생)를 조건으로 검
        bool started = s.maxRegionInfection > 0.05f || s.capturedCount > 0;
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

    // =============================================
    //  조건 키 평가
    //  새 조건을 추가하려면 여기 case만 늘리면 됨
    // =============================================
    private bool EvaluateCondition(string condition, NewsGameState s)
    {
        switch (condition)
        {
            case "STEALTH_LOW":
                return s.stealth <= 3;

            case "COMPLEXITY_LOW":
                return s.complexity <= 3;

            case "INFECTION_LOW":
                return s.infection <= 3;

            case "REGION_INFECTION_40":
                return s.maxRegionInfection >= 0.40f;

            case "BOSS_UNLOCKED":
                return s.anyBossUnlocked;

            case "TREATMENT_STARTED":
                return s.treatmentActive;

            case "TREATMENT_HALF":
                return s.treatmentActive && s.treatmentProgress >= 50f;

            case "TREATMENT_CRITICAL":
                return s.treatmentActive && s.treatmentProgress >= 80f;

            case "COIN_HIGH":
                return s.coins >= 300;

            case "DETECTED_ONCE":
                return s.detectedCount >= 1;

            case "DETECTION_HIGH":
                // 발각 위험이 높은 상태 (치료 진행 중이거나 발각 이력 있음)
                return s.detectedCount >= 1 || (s.treatmentActive && s.treatmentProgress >= 30f);

            case "FIRST_REGION_CAPTURED":
                return s.capturedCount >= 1;

            case "HALF_CAPTURED":
                return s.capturedRatio >= 0.5f && s.capturedRatio < 0.8f;

            case "REGIONS_HALF_CAPTURED":
                return s.capturedRatio >= 0.5f && s.capturedRatio < 0.8f;

            case "INFECTION_STAT_HIGH":
                // 전염도 스탯이 매우 높음 (3스탯 버전 기준 8 이상)
                return s.infection >= 8;

            case "REGIONS_MOSTLY_CAPTURED":
                return s.capturedRatio >= 0.8f && s.capturedRatio < 1f;

            default:
                Debug.LogWarning($"[NewsManager] 알 수 없는 조건: {condition}");
                return false;
        }
    }
}

// =============================================
//  뉴스 조건 판단에 필요한 게임 상태 묶음
//  GameManager가 현재 상태를 채워서 CheckConditions()에 넘김
// =============================================
public struct NewsGameState
{
    public string malwareName;       // 선택한 악성코드 이름 (악성코드 전용 뉴스용)
    public int   infection;          // 현재 전염도
    public int   stealth;            // 현재 은신도
    public int   complexity;         // 현재 복잡도
    public int   coins;              // 현재 코인
    public float maxRegionInfection; // 가장 높은 구역 감염률 (0~1)
    public bool  anyBossUnlocked;    // 보스 침투 가능한 구역 존재 여부
    public bool  treatmentActive;    // 치료 진행 중 여부
    public float treatmentProgress;  // 치료 진행도 (0~100)
    public int   detectedCount;      // 누적 발각 횟수
    public int   capturedCount;      // 점령한 구역 수
    public float capturedRatio;      // 전체 점령 비율 (0~1)
}
