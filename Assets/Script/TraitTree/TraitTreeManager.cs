using System;
using System.Collections.Generic;
using UnityEngine;

namespace TraitTree
{
    public class TraitTreeManager : MonoBehaviour
    {
        public static TraitTreeManager Instance;

        // static: 씬 전환 후에도 데이터 유지 (지역 ID → 언락된 노드 이름 집합)
        static readonly Dictionary<string, HashSet<string>> _unlockedByRegion =
            new Dictionary<string, HashSet<string>>();

        // static: 씬 전환 후에도 데이터 유지 (지역별 카테고리 보너스 캐시)
        static readonly Dictionary<string, Dictionary<TraitCategory, int>> _bonusByRegion =
            new Dictionary<string, Dictionary<TraitCategory, int>>();

        string _currentRegionId = "";
        public event Action OnTreeChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 새 게임 시작 시 호출 — static 데이터를 초기화해 이전 게임 상태가 남지 않도록 함
        public static void ClearStaticData()
        {
            _unlockedByRegion.Clear();
            _bonusByRegion.Clear();
        }

        // ── 현재 편집 지역 ────────────────────────────────────────

        public void SetCurrentRegion(string regionId)
        {
            _currentRegionId = regionId ?? "";
            OnTreeChanged?.Invoke();
        }

        public string CurrentRegionId => _currentRegionId;

        HashSet<string> CurrentUnlockedNames
        {
            get
            {
                if (!_unlockedByRegion.ContainsKey(_currentRegionId))
                    _unlockedByRegion[_currentRegionId] = new HashSet<string>();
                return _unlockedByRegion[_currentRegionId];
            }
        }

        // ── 노드 상태 조회 ────────────────────────────────────────

        // 이름 기반 비교 → 씬 재로드 후 인스턴스가 달라져도 정확히 동작
        public bool IsUnlocked(TraitNode n)
        {
            if (n == null) return false;
            if (!_unlockedByRegion.TryGetValue(_currentRegionId, out var set)) return false;
            return set.Contains(n.name);
        }

        public bool IsAvailable(TraitNode n)
        {
            if (n == null || IsUnlocked(n)) return false;
            if (n.prerequisites == null) return true;
            foreach (var p in n.prerequisites)
                if (!IsUnlocked(p)) return false;
            return true;
        }

        public bool CanAfford(TraitNode n)
        {
            if (n == null || PlayerStats.Instance == null) return false;
            return PlayerStats.Instance.coins >= n.cost;
        }

        public bool CanUnlockNow(TraitNode n) => IsAvailable(n) && CanAfford(n);

        public int GetNextCost(TraitNode n) => n != null ? n.cost         : 0;
        public int GetGain(TraitNode n)     => n != null ? n.effectAmount : 0;

        // 현재 지역의 스탯 = 기본 PlayerStats + 이 지역 노드 보너스
        public int GetCurrentStat(TraitCategory cat)
        {
            int baseVal = 0;
            if (PlayerStats.Instance != null)
                switch (cat)
                {
                    case TraitCategory.Inf:     baseVal = PlayerStats.Instance.inf;     break;
                    case TraitCategory.Comp:    baseVal = PlayerStats.Instance.comp;    break;
                    case TraitCategory.Stealth: baseVal = PlayerStats.Instance.stealth; break;
                }
            return baseVal + GetRegionStatBonus(_currentRegionId, cat);
        }

        // 특정 지역의 카테고리별 누적 보너스 (InfectionEngine / SpreadManager 에서 사용)
        public int GetRegionStatBonus(string regionId, TraitCategory cat)
        {
            if (string.IsNullOrEmpty(regionId)) return 0;
            if (!_bonusByRegion.TryGetValue(regionId, out var bonuses)) return 0;
            bonuses.TryGetValue(cat, out int val);
            return val;
        }

        // ── 언락 ──────────────────────────────────────────────────

        public bool TryUnlock(TraitNode n)
        {
            if (!CanUnlockNow(n)) return false;

            PlayerStats.Instance.AddCoins(-n.cost);
            CurrentUnlockedNames.Add(n.name);

            // 보너스 캐시 업데이트
            AddBonus(_currentRegionId, n.category, n.effectAmount);

            OnTreeChanged?.Invoke();

            // 언락 즉시 저장 — BackToGame() 전에 예외가 발생해도 데이터 보존
            SaveManager.Instance?.Save();
            return true;
        }

        void AddBonus(string regionId, TraitCategory cat, int amount)
        {
            if (!_bonusByRegion.ContainsKey(regionId))
                _bonusByRegion[regionId] = new Dictionary<TraitCategory, int>();
            var b = _bonusByRegion[regionId];
            b[cat] = (b.TryGetValue(cat, out int cur) ? cur : 0) + amount;
        }

        // ── 저장/로드 ─────────────────────────────────────────────

        public List<RegionTraitSaveData> GetAllSaveData()
        {
            var result = new List<RegionTraitSaveData>();
            foreach (var kv in _unlockedByRegion)
            {
                if (kv.Value.Count == 0) continue;
                var item = new RegionTraitSaveData
                {
                    regionId      = kv.Key,
                    unlockedNodes = new List<string>(kv.Value)
                };
                // 보너스 수치를 저장 — ScriptableObject 없는 씬에서도 복원 가능하게
                if (_bonusByRegion.TryGetValue(kv.Key, out var bonuses))
                {
                    bonuses.TryGetValue(TraitCategory.Inf,     out item.infBonus);
                    bonuses.TryGetValue(TraitCategory.Comp,    out item.compBonus);
                    bonuses.TryGetValue(TraitCategory.Stealth, out item.stealthBonus);
                }
                result.Add(item);
            }
            return result;
        }

        public void RestoreFromSaveData(List<RegionTraitSaveData> saveData)
        {
            _unlockedByRegion.Clear();
            _bonusByRegion.Clear();
            if (saveData == null || saveData.Count == 0) return;

            // ScriptableObject로 보너스 재계산 시도 (Process 씬에서만 성공)
            var nodeMap = BuildNodeMap();

            foreach (var entry in saveData)
            {
                if (string.IsNullOrEmpty(entry.regionId) || entry.unlockedNodes == null) continue;
                if (entry.unlockedNodes.Count == 0) continue;

                _unlockedByRegion[entry.regionId] = new HashSet<string>(entry.unlockedNodes);

                // ScriptableObject로 보너스 재계산
                bool anyFound = false;
                foreach (var name in entry.unlockedNodes)
                {
                    if (nodeMap.TryGetValue(name, out var node))
                    {
                        AddBonus(entry.regionId, node.category, node.effectAmount);
                        anyFound = true;
                    }
                }

                // Main 씬처럼 ScriptableObject를 찾지 못한 경우 저장된 수치로 복원
                if (!anyFound)
                {
                    if (entry.infBonus     != 0) AddBonus(entry.regionId, TraitCategory.Inf,     entry.infBonus);
                    if (entry.compBonus    != 0) AddBonus(entry.regionId, TraitCategory.Comp,    entry.compBonus);
                    if (entry.stealthBonus != 0) AddBonus(entry.regionId, TraitCategory.Stealth, entry.stealthBonus);
                }
            }

            OnTreeChanged?.Invoke();
        }

        static Dictionary<string, TraitNode> BuildNodeMap()
        {
            var map = new Dictionary<string, TraitNode>();
            foreach (var node in Resources.FindObjectsOfTypeAll<TraitNode>())
                if (node != null && !string.IsNullOrEmpty(node.name))
                    map[node.name] = node;
            // inactive 패널 포함해서도 탐색
            foreach (var ui in UnityEngine.Object.FindObjectsOfType<TraitNodeUI>(true))
                if (ui?.node != null && !map.ContainsKey(ui.node.name))
                    map[ui.node.name] = ui.node;
            return map;
        }
    }
}
