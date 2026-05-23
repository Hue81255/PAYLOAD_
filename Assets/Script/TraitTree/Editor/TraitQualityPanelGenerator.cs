#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TraitTree.EditorTools
{
    /// <summary>
    /// 선택된 GameObject(보통 ImageEvolve) 하위에 QualityPanel + 5개 TMP 텍스트를 생성한다.
    /// 선택 대상에 TraitTreeView가 부착돼 있으면 필드 자동 연결까지 수행.
    /// 메뉴: PAYLOAD → Create Quality Panel under Selected
    /// </summary>
    public static class TraitQualityPanelGenerator
    {
        [MenuItem("PAYLOAD/Create Quality Panel under Selected")]
        public static void Generate()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("선택 없음",
                    "Hierarchy에서 ImageEvolve(또는 TraitTreeView가 부착된 GameObject)를 선택한 뒤 다시 실행하세요.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Quality Panel");

            // ── QualityPanel 루트 ─────────────────────────────────
            var panelTr = FindDirectChild(selected.transform, "QualityPanel");
            GameObject panel;
            if (panelTr != null)
            {
                panel = panelTr.gameObject;
            }
            else
            {
                panel = new GameObject("QualityPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(selected.transform, false);
                Undo.RegisterCreatedObjectUndo(panel, "Create QualityPanel");

                var prt = (RectTransform)panel.transform;
                // 우측 하단에 280×320 패널을 기본 배치 (사용자가 옮길 수 있음)
                prt.anchorMin = new Vector2(1f, 0f);
                prt.anchorMax = new Vector2(1f, 0f);
                prt.pivot     = new Vector2(1f, 0f);
                prt.anchoredPosition = new Vector2(-20f, 20f);
                prt.sizeDelta = new Vector2(280f, 320f);

                var bgImg = panel.GetComponent<Image>();
                bgImg.color = new Color(0f, 0f, 0f, 0.5f); // 반투명 검정 (디자인에 맞게 변경)
                bgImg.raycastTarget = false;
            }

            // ── 5개 텍스트 ────────────────────────────────────────
            // (parent, name, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta,
            //  initialText, fontSize, alignment, color, style)
            var nameTr = EnsureText(panel.transform, "QualityName",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(-20f, 36f),
                "", 22f, TextAlignmentOptions.TopLeft,
                Color.white, FontStyles.Bold);

            var descTr = EnsureText(panel.transform, "QualityDescription",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -55f), new Vector2(-20f, 130f),
                "", 14f, TextAlignmentOptions.TopLeft,
                new Color(0.85f, 0.85f, 0.85f, 1f), FontStyles.Normal);

            var statTr = EnsureText(panel.transform, "QualityStat",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(-20f, 32f),
                "", 16f, TextAlignmentOptions.MidlineLeft,
                new Color(0.55f, 1f, 0.55f, 1f), FontStyles.Normal);

            var costTr = EnsureText(panel.transform, "QualityCost",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 20f), new Vector2(-20f, 32f),
                "", 16f, TextAlignmentOptions.MidlineLeft,
                new Color(1f, 0.82f, 0.27f, 1f), FontStyles.Normal);

            var emptyTr = EnsureText(panel.transform, "EmptyHint",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(240f, 40f),
                "노드를 선택하세요", 18f, TextAlignmentOptions.Center,
                new Color(0.7f, 0.7f, 0.7f, 1f), FontStyles.Italic);

            // ── TraitTreeView 자동 연결 ───────────────────────────
            var view = selected.GetComponent<TraitTreeView>();
            bool wired = false;
            if (view != null)
            {
                Undo.RecordObject(view, "Wire QualityPanel to TraitTreeView");
                view.qualityRoot        = panel;
                view.qualityEmptyHint   = emptyTr.gameObject;
                view.qualityName        = nameTr.GetComponent<TMP_Text>();
                view.qualityDescription = descTr.GetComponent<TMP_Text>();
                view.qualityStat        = statTr.GetComponent<TMP_Text>();
                view.qualityCost        = costTr.GetComponent<TMP_Text>();
                if (PrefabUtility.IsPartOfPrefabInstance(view))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                EditorUtility.SetDirty(view);
                wired = true;
            }

            Undo.CollapseUndoOperations(undoGroup);

            string msg = $"부모: {selected.name}\n" +
                         $"QualityPanel + 텍스트 5개 생성/갱신.\n" +
                         (wired
                            ? "TraitTreeView 필드 자동 연결 완료."
                            : "이 GameObject에 TraitTreeView가 없어 자동 연결은 건너뛰었습니다.");
            EditorUtility.DisplayDialog("Quality 패널 생성 완료", msg, "OK");
            Debug.Log($"[QualityPanelGenerator] 부모={selected.name}, 자동연결={wired}");
        }

        // ── 헬퍼 ────────────────────────────────────────────────────

        static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        /// <summary>
        /// 이름의 자식이 이미 있으면 그것을 재사용해 속성만 갱신, 없으면 신규 생성.
        /// sizeDelta의 x가 음수면 stretch anchor에서 좌우 여백을 의미한다
        /// (예: -20 = 10px L + 10px R 여백).
        /// </summary>
        static Transform EnsureText(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            string initialText, float fontSize, TextAlignmentOptions alignment,
            Color color, FontStyles style)
        {
            var existing = FindDirectChild(parent, name);
            GameObject go;
            TextMeshProUGUI tmp;

            if (existing != null)
            {
                go = existing.gameObject;
                tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = Undo.AddComponent<TextMeshProUGUI>(go);
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Quality Text");
                tmp = Undo.AddComponent<TextMeshProUGUI>(go);
            }

            Undo.RecordObject(tmp, "Configure Quality Text");

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;

            tmp.text       = initialText;
            tmp.fontSize   = fontSize;
            tmp.alignment  = alignment;
            tmp.color      = color;
            tmp.fontStyle  = style;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget      = false;

            EditorUtility.SetDirty(tmp);
            return rt;
        }
    }
}
#endif
