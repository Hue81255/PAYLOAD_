using System;
using System.Collections.Generic;
using UnityEngine;

namespace TraitTree
{
    public class TraitTreeManager : MonoBehaviour
    {
        public static TraitTreeManager Instance;

        // 지역 ID → 언락된 노드 집합
        readonly Dictionary<string, HashSet<TraitNode>> _unlockedByRegion =
            new Dictionary<string, HashSet<TraitNode>>();

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

        // ── 현재 편집 지역 ────────────────────────────────────────

        public void SetCurrentRegion(string regionId)
        {
            _currentRegionId = regionId ?? "";
            OnTreeChanged?.Invoke();
        }

        public string CurrentRegionId => _currentRegionId;

        HashSet<TraitNode> CurrentUnlocked
        {
            get
            {
                if (!_unlockedByRegion.ContainsKey(_currentRegionId))
                    _unlockedByRegion[_currentRegionId] = new HashSet<TraitNode>();
                return _unlockedByRegion[_currentRegionId];
            }
        }

        // ── 노드 상태 조회 ────────────────────────────────────────

        public bool IsUnlocked(TraitNode n) => n != null && CurrentUnlocked.Contains(n);

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
            if (string.IsNullOrEmpty(regionId) || !_unlockedByRegion.ContainsKey(regionId))
                return 0;
            int bonus = 0;
            foreach (var n in _unlockedByRegion[regionId])
                if (n != null && n.category == cat) bonus += n.effectAmount;
            return bonus;
        }

        // ── 언락 ──────────────────────────────────────────────────

        public bool TryUnlock(TraitNode n)
        {
            if (!CanUnlockNow(n)) return false;

            PlayerStats.Instance.AddCoins(-n.cost);
            // 글로벌 스탯 변경 없음 — 지역 전용 보너스로만 반영
            CurrentUnlocked.Add(n);
            OnTreeChanged?.Invoke();
            return true;
        }

        // ── 저장/로드 ─────────────────────────────────────────────

        public List<RegionTraitSaveData> GetAllSaveData()
        {
            var result = new List<RegionTraitSaveData>();
            foreach (var kv in _unlockedByRegion)
            {
                if (kv.Value.Count == 0) continue;
                var entry = new RegionTraitSaveData
                {
                    regionId      = kv.Key,
                    unlockedNodes = new List<string>()
                };
                foreach (var node in kv.Value)
                    if (node != null) entry.unlockedNodes.Add(node.name);
                result.Add(entry);
            }
            return result;
        }

        public void RestoreFromSaveData(List<RegionTraitSaveData> saveData)
        {
            _unlockedByRegion.Clear();
            if (saveData == null) return;

            var nodeMap = new Dictionary<string, TraitNode>();
            foreach (var ui in UnityEngine.Object.FindObjectsOfType<TraitNodeUI>())
                if (ui.node != null) nodeMap[ui.node.name] = ui.node;

            foreach (var entry in saveData)
            {
                if (string.IsNullOrEmpty(entry.regionId) || entry.unlockedNodes == null) continue;
                var set = new HashSet<TraitNode>();
                foreach (var name in entry.unlockedNodes)
                    if (nodeMap.TryGetValue(name, out var node))
                        set.Add(node);
                if (set.Count > 0)
                    _unlockedByRegion[entry.regionId] = set;
            }

            OnTreeChanged?.Invoke();
        }
    }
}
