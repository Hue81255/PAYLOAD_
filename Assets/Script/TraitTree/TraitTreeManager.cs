using System;
using System.Collections.Generic;
using UnityEngine;

namespace TraitTree
{
    public class TraitTreeManager : MonoBehaviour
    {
        public static TraitTreeManager Instance;

        readonly HashSet<TraitNode> unlocked = new HashSet<TraitNode>();
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

        public bool IsUnlocked(TraitNode n) => n != null && unlocked.Contains(n);

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

        // 이제 노드 자체에서 직접 읽음 (EvolutionManager 의존성 제거)
        public int GetNextCost(TraitNode n) => n != null ? n.cost         : 0;
        public int GetGain(TraitNode n)     => n != null ? n.effectAmount : 0;

        public int GetCurrentStat(TraitCategory cat)
        {
            if (PlayerStats.Instance == null) return 0;
            switch (cat)
            {
                case TraitCategory.Inf:     return PlayerStats.Instance.inf;
                case TraitCategory.Comp:    return PlayerStats.Instance.comp;
                case TraitCategory.Stealth: return PlayerStats.Instance.stealth;
            }
            return 0;
        }

        /// <summary>
        /// 노드를 해제한다. 코인 차감과 스탯 증가를 PlayerStats에 직접 반영하므로
        /// EvolutionManager 인스턴스가 없어도 동작한다.
        /// (PlayerStats.UpgradeX는 내부에서 InfectionEngine 동기화까지 처리해줌.)
        /// </summary>
        public bool TryUnlock(TraitNode n)
        {
            if (!CanUnlockNow(n)) return false;

            PlayerStats.Instance.AddCoins(-n.cost);

            switch (n.category)
            {
                case TraitCategory.Inf:     PlayerStats.Instance.UpgradeInf(n.effectAmount);     break;
                case TraitCategory.Comp:    PlayerStats.Instance.UpgradeComp(n.effectAmount);    break;
                case TraitCategory.Stealth: PlayerStats.Instance.UpgradeStealth(n.effectAmount); break;
            }

            unlocked.Add(n);
            OnTreeChanged?.Invoke();
            return true;
        }

        public List<string> GetUnlockedNames()
        {
            var names = new List<string>();
            foreach (var n in unlocked)
                if (n != null) names.Add(n.name);
            return names;
        }

        // 씬 로드 후 저장된 이름 목록으로 언락 상태를 복원한다.
        // TraitNodeUI 컴포넌트를 씬에서 탐색해 이름으로 ScriptableObject를 역추적한다.
        public void RestoreFromNames(List<string> names)
        {
            unlocked.Clear();
            if (names == null || names.Count == 0) return;

            var nodeMap = new Dictionary<string, TraitNode>();
            foreach (var ui in UnityEngine.Object.FindObjectsOfType<TraitNodeUI>())
                if (ui.node != null) nodeMap[ui.node.name] = ui.node;

            foreach (var n in names)
                if (nodeMap.TryGetValue(n, out var node))
                    unlocked.Add(node);

            OnTreeChanged?.Invoke();
        }
    }
}
