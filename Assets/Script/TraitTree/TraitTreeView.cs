using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TraitTree
{
    /// <summary>
    /// ImageEvolve 패널에 부착. 자식들 중에 있는 모든 TraitNodeUI를 자동 수집해서
    /// 클릭/더블클릭 라우팅, Quality 텍스트, Progress 슬라이더 미리보기를 처리한다.
    /// 기존 코드(PlayerStats/EvolutionManager/UIManager 등)는 일절 수정하지 않는다.
    /// </summary>
    public class TraitTreeView : MonoBehaviour
    {
        [Header("Quality Panel (선택된 노드 정보)")]
        public GameObject qualityRoot;       // 비어있을 때 숨길 영역
        public GameObject qualityEmptyHint;  // "노드를 선택하세요"
        public TMP_Text qualityName;
        public TMP_Text qualityDescription;
        public TMP_Text qualityStat;
        public TMP_Text qualityCost;

        [Header("Progress Sliders")]
        public TraitStatSlider infSlider;
        public TraitStatSlider compSlider;
        public TraitStatSlider stealthSlider;

        [Header("Line Connectors")]
        [Tooltip("커넥터를 둘 부모. 비워두면 TraitTreeView의 transform을 사용한다.")]
        public RectTransform connectorParent;
        public float connectorThickness = 4f;
        public Color connectorLockedColor   = new Color(1f, 1f, 1f, 0.15f);
        public Color connectorReadyColor    = new Color(0.30f, 0.55f, 1f, 0.9f);
        public Color connectorUnlockedColor = new Color(1f, 0.8f, 0.2f, 0.9f);

        [Header("Arrow Head")]
        public bool  arrowHeadEnabled = true;
        public float arrowHeadLength  = 12f;
        public float arrowHeadAngle   = 30f;
        public float arrowHeadInset   = 8f;

        [Header("Slider Colors (전염도=빨강 / 복잡도=파랑 / 은신도=초록)")]
        public Color infColor     = new Color(1f,    0.25f, 0.25f, 1f);
        public Color compColor    = new Color(0.25f, 0.50f, 1f,    1f);
        public Color stealthColor = new Color(0.25f, 0.85f, 0.40f, 1f);
        [Tooltip("미리보기는 actual 색의 어두운 버전 (밝기 곱하기 계수)")]
        [Range(0.05f, 1f)] public float previewDarkness = 0.35f;

        [Header("Debug")]
        [Tooltip("켜면 클릭/언락/슬라이더 갱신 흐름을 Console에 로그한다.")]
        public bool debugLogs = false;

        readonly List<TraitNodeUI>    nodes      = new List<TraitNodeUI>();
        readonly List<TraitConnector> connectors = new List<TraitConnector>();
        TraitNodeUI selected;
        bool needsLineLayout = true;

        void Awake()
        {
            nodes.Clear();
            GetComponentsInChildren(true, nodes);
            foreach (var n in nodes) n.Bind(this);
            ApplySliderColors();
            BuildConnectors();
        }

        void ApplySliderColors()
        {
            if (infSlider     != null) infSlider.ApplyFillColors(infColor,     Darken(infColor));
            if (compSlider    != null) compSlider.ApplyFillColors(compColor,   Darken(compColor));
            if (stealthSlider != null) stealthSlider.ApplyFillColors(stealthColor, Darken(stealthColor));
        }

        Color Darken(Color c) =>
            new Color(c.r * previewDarkness, c.g * previewDarkness, c.b * previewDarkness, c.a);

        void BuildConnectors()
        {
            // 이미 만들어졌다면 재생성 안 함 (OnEnable 재호출 대비)
            if (connectors.Count > 0) return;

            var parent = connectorParent != null ? connectorParent : (RectTransform)transform;

            // node 객체 → UI 매핑 (prerequisite 노드 ScriptableObject로 UI를 역추적)
            var map = new Dictionary<TraitNode, TraitNodeUI>();
            foreach (var ui in nodes)
                if (ui != null && ui.node != null) map[ui.node] = ui;

            foreach (var toUI in nodes)
            {
                if (toUI == null || toUI.node == null || toUI.node.prerequisites == null) continue;
                foreach (var prereq in toUI.node.prerequisites)
                {
                    if (prereq == null) continue;
                    if (!map.TryGetValue(prereq, out var fromUI)) continue;

                    var go = new GameObject($"Connector_{prereq.name}_to_{toUI.node.name}",
                                            typeof(RectTransform), typeof(TraitConnector));
                    go.transform.SetParent(parent, false);
                    // 노드보다 먼저 그려지도록 맨 뒤로 보냄
                    go.transform.SetSiblingIndex(0);

                    var c = go.GetComponent<TraitConnector>();
                    c.thickness        = connectorThickness;
                    c.lockedColor      = connectorLockedColor;
                    c.readyColor       = connectorReadyColor;
                    c.unlockedColor    = connectorUnlockedColor;
                    c.arrowHeadEnabled = arrowHeadEnabled;
                    c.arrowHeadLength  = arrowHeadLength;
                    c.arrowHeadAngle   = arrowHeadAngle;
                    c.arrowHeadInset   = arrowHeadInset;
                    c.Bind(fromUI, toUI);
                    connectors.Add(c);
                }
            }
        }

        void LateUpdate()
        {
            if (!needsLineLayout || connectors.Count == 0) return;
            Canvas.ForceUpdateCanvases();
            foreach (var c in connectors) if (c != null) c.UpdateLine();
            needsLineLayout = false;
        }

        void OnEnable()
        {
            if (TraitTreeManager.Instance != null)
                TraitTreeManager.Instance.OnTreeChanged += RefreshAll;
            needsLineLayout = true; // 패널 다시 켤 때 위치 재계산
            ShowEmptyQuality();
            RefreshAll();
        }

        void OnDisable()
        {
            if (TraitTreeManager.Instance != null)
                TraitTreeManager.Instance.OnTreeChanged -= RefreshAll;
            ClearSliderPreview();
        }

        // ── 노드 콜백 ──────────────────────────────────────────────

        public void OnNodeClick(TraitNodeUI ui)
        {
            if (selected == ui)
            {
                // 같은 노드 재클릭 → 선택 해제
                selected = null;
                ClearSliderPreview();
                ShowEmptyQuality();
                RefreshAll();
                return;
            }
            selected = ui;
            RefreshAll();
            ShowQuality(ui.node);
            ShowSliderPreview(ui.node);
        }

        public void OnNodeDoubleClick(TraitNodeUI ui)
        {
            if (TraitTreeManager.Instance == null)
            {
                if (debugLogs) Debug.LogError("[TraitTreeView] TraitTreeManager.Instance가 null입니다. Scene에 매니저 GameObject가 있는지 확인하세요.");
                return;
            }

            if (debugLogs)
            {
                int curStat = TraitTreeManager.Instance.GetCurrentStat(ui.node.category);
                int cost    = TraitTreeManager.Instance.GetNextCost(ui.node);
                int gain    = TraitTreeManager.Instance.GetGain(ui.node);
                int coins   = PlayerStats.Instance != null ? PlayerStats.Instance.coins : -1;
                bool avail  = TraitTreeManager.Instance.IsAvailable(ui.node);
                bool afford = TraitTreeManager.Instance.CanAfford(ui.node);
                Debug.Log($"[TraitTreeView] DoubleClick {ui.node.name} | category={ui.node.category} " +
                          $"currentStat={curStat} cost={cost} gain={gain} coins={coins} " +
                          $"available={avail} afford={afford}");
            }

            if (!TraitTreeManager.Instance.CanUnlockNow(ui.node))
            {
                UIManager.Instance?.ShowWarning("⚠️ 코인이 부족하거나 선행 특성이 필요합니다");
                if (debugLogs) Debug.LogWarning($"[TraitTreeView] CanUnlockNow 실패 → 언락 안 함");
                return;
            }

            bool ok = TraitTreeManager.Instance.TryUnlock(ui.node);
            if (debugLogs) Debug.Log($"[TraitTreeView] TryUnlock 결과={ok}, after stat={TraitTreeManager.Instance.GetCurrentStat(ui.node.category)}");

            // 언락 후 슬라이더는 실제값으로 갱신, Quality는 "해제됨" 표시로 갱신
            ClearSliderPreview();
            RefreshAll();
            ShowQuality(ui.node);
        }

        // ── 갱신 ──────────────────────────────────────────────────

        void RefreshAll()
        {
            var nextSet = ComputeNextOfSelected();
            foreach (var n in nodes)
            {
                if (n == null) continue;
                bool isSelected = (selected != null && selected == n);
                bool isNext     = (n.node != null && nextSet.Contains(n.node));
                n.Refresh(isSelected, isNext);
            }
            foreach (var c in connectors) if (c != null) c.Refresh();
            RefreshSliderActuals();
        }

        HashSet<TraitNode> ComputeNextOfSelected()
        {
            var result = new HashSet<TraitNode>();
            if (selected == null || selected.node == null) return result;
            foreach (var n in nodes)
            {
                if (n == null || n.node == null) continue;
                if (n.node.prerequisites != null && n.node.prerequisites.Contains(selected.node))
                    result.Add(n.node);
            }
            return result;
        }

        // ── Quality 창 ─────────────────────────────────────────────

        void ShowQuality(TraitNode n)
        {
            if (n == null || TraitTreeManager.Instance == null) { ShowEmptyQuality(); return; }
            if (qualityRoot      != null) qualityRoot.SetActive(true);
            if (qualityEmptyHint != null) qualityEmptyHint.SetActive(false);

            int gain     = TraitTreeManager.Instance.GetGain(n);
            int cost     = TraitTreeManager.Instance.GetNextCost(n);
            int current  = TraitTreeManager.Instance.GetCurrentStat(n.category);
            bool isOn    = TraitTreeManager.Instance.IsUnlocked(n);

            if (qualityName != null)        qualityName.text = n.displayName;
            if (qualityDescription != null) qualityDescription.text = n.description;
            if (qualityStat != null)
                qualityStat.text = isOn
                    ? $"{CategoryKor(n.category)}: {current}"
                    : $"{CategoryKor(n.category)}: {current} → {current + gain} (+{gain})";
            if (qualityCost != null)
                qualityCost.text = isOn ? "이미 해제됨" : $"비용: {cost} 코인";
        }

        void ShowEmptyQuality()
        {
            if (qualityEmptyHint != null) qualityEmptyHint.SetActive(true);
            if (qualityName        != null) qualityName.text = "";
            if (qualityDescription != null) qualityDescription.text = "";
            if (qualityStat        != null) qualityStat.text = "";
            if (qualityCost        != null) qualityCost.text = "";
        }

        // ── 슬라이더 ───────────────────────────────────────────────

        void ShowSliderPreview(TraitNode n)
        {
            ClearSliderPreview();
            if (n == null || TraitTreeManager.Instance == null) return;
            if (TraitTreeManager.Instance.IsUnlocked(n)) return;
            int gain = TraitTreeManager.Instance.GetGain(n);
            switch (n.category)
            {
                case TraitCategory.Inf:     if (infSlider     != null) infSlider.ShowPreview(gain);     break;
                case TraitCategory.Comp:    if (compSlider    != null) compSlider.ShowPreview(gain);    break;
                case TraitCategory.Stealth: if (stealthSlider != null) stealthSlider.ShowPreview(gain); break;
            }
        }

        void ClearSliderPreview()
        {
            if (infSlider     != null) infSlider.ClearPreview();
            if (compSlider    != null) compSlider.ClearPreview();
            if (stealthSlider != null) stealthSlider.ClearPreview();
        }

        void RefreshSliderActuals()
        {
            if (PlayerStats.Instance == null)
            {
                if (debugLogs) Debug.LogWarning("[TraitTreeView] RefreshSliderActuals: PlayerStats.Instance == null → 슬라이더 갱신 불가");
                return;
            }
            if (infSlider     != null) infSlider.SetActual(PlayerStats.Instance.inf);
            if (compSlider    != null) compSlider.SetActual(PlayerStats.Instance.comp);
            if (stealthSlider != null) stealthSlider.SetActual(PlayerStats.Instance.stealth);

            if (debugLogs)
                Debug.Log($"[TraitTreeView] sliders ← inf={PlayerStats.Instance.inf}, " +
                          $"comp={PlayerStats.Instance.comp}, stealth={PlayerStats.Instance.stealth}");
        }

        static string CategoryKor(TraitCategory c)
        {
            switch (c)
            {
                case TraitCategory.Inf:     return "전염도";
                case TraitCategory.Comp:    return "복잡도";
                case TraitCategory.Stealth: return "은신도";
            }
            return "";
        }
    }
}
