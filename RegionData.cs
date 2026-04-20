// =============================================
// RegionData.cs
// 대구 9개 구/군 방어스탯 및 지역 환경 수치
// 기획서 "지역별 특성" 기반으로 설계
// =============================================

using UnityEngine;
using System.Collections.Generic;

// 지역 타입 (기획서: 주거 < 산업단지 < 업무 순 난이도)
public enum RegionType
{
    Residential,            // 주거
    Industrial,             // 산업단지
    ResidentialIndustrial,  // 주거+산업단지
    ResidentialBusiness,    // 주거+업무
    Business                // 업무 (최고 난이도)
}

// 지역 난이도 등급
public enum RegionDifficulty
{
    Easy,       // 쉬움
    Normal,     // 보통
    Hard,       // 어려움
    VeryHard    // 매우 어려움
}

// =============================================
//  지역 방어 스탯 데이터
// =============================================
[System.Serializable]
public class RegionDefenseData
{
    public string           regionName;
    public RegionType       regionType;
    public RegionDifficulty difficulty;

    // ── 침투 최소 요구 스탯 ──
    // 플레이어 스탯이 이 수치 미만이면 침투 불가
    public float minInfectionSpeed;     // 최소 감염속도
    public float minStealth;            // 최소 탐지회피
    public float minStrength;           // 최소 감염강도
    public float minNetworkPenetration; // 최소 네트워크침투력

    // ── 지역 환경 수치 (확산 확률 공식에 사용) ──
    // securityLevel: 0~1, 높을수록 확산 어려움
    // connectionStrength: 0~1, 높을수록 인접 구역 전파 쉬움
    public float securityLevel;
    public float connectionStrength;

    // ── 보스 기관 방어스탯 (일반 수치보다 +20~30 높음) ──
    public TargetDefenseStats bossDefense;

    // ── 인접 지역 목록 (확산 경로) ──
    public List<string> adjacentRegions;
}

// =============================================
//  9개 구/군 데이터 초기화
//  RegionManager에서 이 클래스를 참조하여 사용
// =============================================
public static class RegionDatabase
{
    public static List<RegionDefenseData> All => new List<RegionDefenseData>
    {
        // ─────────────────────────────────────────
        //  달성군1 (하빈, 다사 위쪽)
        //  산업단지 중심 / 모바일 기기 많음
        //  기획서: "타 지역보다 모바일 기기 많음, 전체적 침투 난이도 낮음"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "달성군1",
            regionType        = RegionType.Industrial,
            difficulty        = RegionDifficulty.Easy,
            minInfectionSpeed     = 15f,
            minStealth            = 10f,
            minStrength           = 10f,
            minNetworkPenetration = 20f,
            securityLevel         = 0.25f,
            connectionStrength    = 0.35f,
            bossDefense = new TargetDefenseStats {
                minInfectionSpeed = 35f, minStealth = 30f,
                minStrength = 30f, minNetworkPenetration = 40f
            },
            adjacentRegions = new List<string> { "달성군2", "달서구", "서구" }
        },

        // ─────────────────────────────────────────
        //  달성군2 (아래쪽)
        //  산업단지 중심 / 인구 적음
        //  기획서: "타 지역보다 모바일 기기 많음, 침투 난이도 낮음"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "달성군2",
            regionType        = RegionType.Industrial,
            difficulty        = RegionDifficulty.Easy,
            minInfectionSpeed     = 15f,
            minStealth            = 10f,
            minStrength           = 10f,
            minNetworkPenetration = 20f,
            securityLevel         = 0.28f,
            connectionStrength    = 0.38f,
            bossDefense = new TargetDefenseStats {
                minInfectionSpeed = 35f, minStealth = 30f,
                minStrength = 30f, minNetworkPenetration = 40f
            },
            adjacentRegions = new List<string> { "달성군1", "달서구", "남구" }
        },

        // ─────────────────────────────────────────
        //  서구
        //  산업단지 중심 / 인구 적음 / IOT 기기 수 적음
        //  기획서: "적은 인구수, 많은 산업단지로 모바일 기기 수 적음"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "서구",
            regionType        = RegionType.Industrial,
            difficulty        = RegionDifficulty.Easy,
            minInfectionSpeed     = 18f,
            minStealth            = 12f,
            minStrength           = 12f,
            minNetworkPenetration = 22f,
            securityLevel         = 0.30f,
            connectionStrength    = 0.40f,
            bossDefense = new TargetDefenseStats {
                minInfectionSpeed = 38f, minStealth = 32f,
                minStrength = 32f, minNetworkPenetration = 42f
            },
            adjacentRegions = new List<string> { "달성군1", "북구", "중구", "남구" }
        },

        // ─────────────────────────────────────────
        //  남구
        //  주거 중심 / 모바일·IOT 기기 주력
        //  기획서: "주거 중심 지역, 방어스탯 낮은 편"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "남구",
            regionType        = RegionType.Residential,
            difficulty        = RegionDifficulty.Normal,
            minInfectionSpeed     = 22f,
            minStealth            = 18f,
            minStrength           = 15f,
            minNetworkPenetration = 25f,
            securityLevel         = 0.38f,
            connectionStrength    = 0.45f,
            bossDefense = new TargetDefenseStats {
                minInfectionSpeed = 42f, minStealth = 38f,
                minStrength = 35f, minNetworkPenetration = 45f
            },
            adjacentRegions = new List<string> { "서구", "중구", "수성구", "달성군2" }
        },

        // ─────────────────────────────────────────
        //  달서구
        //  산업단지+주거 / 대구 최대 인구
        //  기획서: "매우매우 많은 모바일+IOT 기기, 난이도와 리워드는 보통"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "달서구",
            regionType        = RegionType.ResidentialIndustrial,
            difficulty        = RegionDifficulty.Normal,
            minInfectionSpeed     = 25f,
            minStealth            = 20f,
            minStrength           = 18f,
            minNetworkPenetration = 28f,
            securityLevel         = 0.42f,
            connectionStrength    = 0.50f,
            bossDefense = new TargetDefenseStats {
                minInfectionSpeed = 45f, minStealth = 40f,
                minStrength = 38f, minNetworkPenetration = 48f
            },
            adjacentRegions = new List<string> { "달성군1", "달성군2", "서구", "북구", "남구" }
        },

        // ─────────────────────────────────────────
        //  북구
        //  주거+업무 / 금호강 기준 남북 생활권 분리
        //  기획서: "금호강 기준으로 북쪽은 방어스탯 낮음, 남쪽은 높음"
        //  → 평균값으로 설계 (RegionManager에서 남/북 서브존 분리 가능)
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "북구",
            regionType        = RegionType.ResidentialBusiness,
            difficulty        = RegionDifficulty.Hard,
            minInfectionSpeed     = 30f,
            minStealth            = 28f,
            minStrength           = 22f,
            minNetworkPenetration = 35f,
            securityLevel         = 0.55f,
            connectionStrength    = 0.62f,
            bossDefense = new TargetDefenseStats {
                minInfectionSpeed = 52f, minStealth = 50f,
                minStrength = 45f, minNetworkPenetration = 58f
            },
            adjacentRegions = new List<string> { "서구", "달서구", "중구", "동구" }
        },

        // ─────────────────────────────────────────
        //  동구
        //  주거+업무 / 대구신서혁신도시 등 고가치 기관 다수
        //  기획서: "고가치 기관들은 난이도 높고 그 외 동구 지역들은 낮게 설정"
        //  → 평균값으로 설계 (고가치 기관은 보스 스탯으로 처리)
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "동구",
            regionType        = RegionType.ResidentialBusiness,
            difficulty        = RegionDifficulty.Hard,
            minInfectionSpeed     = 32f,
            minStealth            = 30f,
            minStrength           = 25f,
            minNetworkPenetration = 38f,
            securityLevel         = 0.60f,
            connectionStrength    = 0.65f,
            bossDefense = new TargetDefenseStats {
                // 대구신서혁신도시, 경북첨단의료복합단지 등 고가치 기관
                minInfectionSpeed = 60f, minStealth = 58f,
                minStrength = 50f, minNetworkPenetration = 65f
            },
            adjacentRegions = new List<string> { "북구", "중구", "수성구" }
        },

        // ─────────────────────────────────────────
        //  수성구
        //  주거+업무 / 많은 정보통신기기 보유
        //  기획서: "중구보다 낮은 난이도, 요구 스탯·난이도·리워드 높게"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "수성구",
            regionType        = RegionType.ResidentialBusiness,
            difficulty        = RegionDifficulty.Hard,
            minInfectionSpeed     = 35f,
            minStealth            = 35f,
            minStrength           = 28f,
            minNetworkPenetration = 40f,
            securityLevel         = 0.65f,
            connectionStrength    = 0.68f,
            bossDefense = new TargetDefenseStats {
                // 대구지방법원, 대구고등법원, 대구지방검찰청 등
                minInfectionSpeed = 62f, minStealth = 62f,
                minStrength = 55f, minNetworkPenetration = 68f
            },
            adjacentRegions = new List<string> { "동구", "중구", "남구", "달성군2" }
        },

        // ─────────────────────────────────────────
        //  중구 ★ 최고 난이도
        //  업무지역 / 실거주 인구 적음
        //  기획서: "가장 난이도 높은 지역, 방어스탯++ 위험도++ 리워드++"
        //          "중구 내 모든 정보통신기기 방어스탯++ 발각위험도++ 리워드++"
        // ─────────────────────────────────────────
        new RegionDefenseData {
            regionName        = "중구",
            regionType        = RegionType.Business,
            difficulty        = RegionDifficulty.VeryHard,
            minInfectionSpeed     = 45f,
            minStealth            = 50f,
            minStrength           = 38f,
            minNetworkPenetration = 55f,
            securityLevel         = 0.85f,
            connectionStrength    = 0.80f,
            bossDefense = new TargetDefenseStats {
                // 대구광역시청 동인청사, 한국은행 대구지점 등
                minInfectionSpeed = 72f, minStealth = 78f,
                minStrength = 65f, minNetworkPenetration = 82f
            },
            adjacentRegions = new List<string> { "북구", "동구", "서구", "남구", "수성구" }
        },
    };

    // =============================================
    //  이름으로 지역 데이터 조회
    // =============================================
    public static RegionDefenseData GetByName(string name)
    {
        return All.Find(r => r.regionName == name);
    }

    // =============================================
    //  난이도별 지역 목록 조회
    // =============================================
    public static List<RegionDefenseData> GetByDifficulty(RegionDifficulty diff)
    {
        return All.FindAll(r => r.difficulty == diff);
    }

    // =============================================
    //  침투 가능 여부 판단 (PlayerStats 기반)
    //  InfectionEngine.CanInfect()과 함께 사용
    // =============================================
    public static bool CanEnterRegion(PlayerStats stats, string regionName)
    {
        var region = GetByName(regionName);
        if (region == null) return false;

        return stats.infectionSpeed     >= region.minInfectionSpeed
            && stats.stealthRate        >= region.minStealth
            && stats.infectionStrength  >= region.minStrength
            && stats.networkPenetration >= region.minNetworkPenetration;
    }
}
