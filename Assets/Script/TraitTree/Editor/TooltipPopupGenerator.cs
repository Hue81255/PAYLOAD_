#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TraitTree.EditorTools
{
    /// <summary>
    /// Canvas 아래에 TooltipPopup UI 구조를 자동 생성하고
    /// 씬 안의 "ButtonTooltip"을 찾아 OnClick → Show로 자동 와이어링한다.
    /// 메뉴: PAYLOAD → Create Tooltip Popup in Scene
    /// </summary>
    public static class TooltipPopupGenerator
    {
        const string PopupRootName = "TooltipPopup";

        [MenuItem("PAYLOAD/Create Tooltip Popup in Scene")]
        public static void Generate()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Canvas 없음",
                    "현재 씬에 Canvas가 없습니다. UI Canvas를 먼저 만들어주세요.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Tooltip Popup");

            // 이미 있으면 재사용
            var existing = canvas.transform.Find(PopupRootName);
            GameObject rootGO;
            if (existing != null)
            {
                rootGO = existing.gameObject;
            }
            else
            {
                rootGO = CreateRect(canvas.transform, PopupRootName);
                StretchFill((RectTransform)rootGO.transform);
                Undo.RegisterCreatedObjectUndo(rootGO, "Create TooltipPopup root");
                // 다른 UI 위에 그려지도록 마지막 sibling으로 보냄
                rootGO.transform.SetAsLastSibling();
            }

            // ── PopupContent (Show/Hide 토글 대상) ─────────────────
            var contentTr = rootGO.transform.Find("PopupContent");
            GameObject content;
            if (contentTr != null)
            {
                content = contentTr.gameObject;
            }
            else
            {
                content = CreateRect(rootGO.transform, "PopupContent");
                StretchFill((RectTransform)content.transform);
                Undo.RegisterCreatedObjectUndo(content, "Create PopupContent");
            }

            // ── Dim 배경 (클릭 → 닫기) ─────────────────────────────
            var dim = EnsureChild(content.transform, "Dim",
                addImage: true, addButton: true);
            StretchFill((RectTransform)dim.transform);
            var dimImg = dim.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.6f);
            dimImg.raycastTarget = true;

            // ── PopupBox (중앙 박스) ───────────────────────────────
            var box = EnsureChild(content.transform, "PopupBox",
                addImage: true, addButton: false);
            var boxRT = (RectTransform)box.transform;
            boxRT.anchorMin = new Vector2(0.5f, 0.5f);
            boxRT.anchorMax = new Vector2(0.5f, 0.5f);
            boxRT.pivot     = new Vector2(0.5f, 0.5f);
            boxRT.anchoredPosition = Vector2.zero;
            boxRT.sizeDelta = new Vector2(600f, 360f);
            var boxImg = box.GetComponent<Image>();
            boxImg.color = new Color(0.13f, 0.14f, 0.20f, 0.97f);
            boxImg.raycastTarget = true; // dim 클릭이 박스를 뚫지 않게

            // ── Title ──────────────────────────────────────────────
            var titleTr = EnsureTMPText(box.transform, "Title", "도움말",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(-40f, 50f),
                28f, TextAlignmentOptions.Center,
                Color.white, FontStyles.Bold);

            // ── Body ───────────────────────────────────────────────
            var bodyTr = EnsureTMPText(box.transform, "Body", "",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(-60f, -130f),
                18f, TextAlignmentOptions.TopLeft,
                new Color(0.9f, 0.9f, 0.92f, 1f), FontStyles.Normal);

            // ── CloseButton ────────────────────────────────────────
            var closeBtnGO = EnsureChild(box.transform, "CloseButton",
                addImage: true, addButton: true);
            var closeRT = (RectTransform)closeBtnGO.transform;
            closeRT.anchorMin = new Vector2(0.5f, 0f);
            closeRT.anchorMax = new Vector2(0.5f, 0f);
            closeRT.pivot     = new Vector2(0.5f, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 20f);
            closeRT.sizeDelta = new Vector2(140f, 44f);
            var closeImg = closeBtnGO.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.40f, 0.85f, 1f);
            closeImg.raycastTarget = true;

            var closeLabelTr = EnsureTMPText(closeBtnGO.transform, "Label", "확인",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                18f, TextAlignmentOptions.Center,
                Color.white, FontStyles.Bold);
            var closeLabel = closeLabelTr.GetComponent<TextMeshProUGUI>();
            if (closeLabel != null) closeLabel.raycastTarget = false;

            // ── TooltipPopup 컴포넌트 부착 & 참조 연결 ─────────────
            var popup = rootGO.GetComponent<TooltipPopup>();
            if (popup == null) popup = Undo.AddComponent<TooltipPopup>(rootGO);

            Undo.RecordObject(popup, "Configure TooltipPopup");
            popup.popupRoot   = content;
            popup.bodyText    = bodyTr.GetComponent<TextMeshProUGUI>();
            popup.startHidden = true;
            EditorUtility.SetDirty(popup);

            // ── 버튼 이벤트 연결 (persistent listener) ─────────────
            var closeButton = closeBtnGO.GetComponent<Button>();
            var dimButton   = dim.GetComponent<Button>();
            WirePersistentClick(closeButton, popup, nameof(TooltipPopup.Hide));
            WirePersistentClick(dimButton,   popup, nameof(TooltipPopup.Hide));

            // ButtonTooltip 자동 와이어링
            bool wiredOpen = false;
            var tooltipBtnGO = GameObject.Find("ButtonTooltip");
            if (tooltipBtnGO != null)
            {
                var tooltipBtn = tooltipBtnGO.GetComponent<Button>();
                if (tooltipBtn != null)
                {
                    WirePersistentClick(tooltipBtn, popup, nameof(TooltipPopup.Show));
                    wiredOpen = true;
                }
            }

            // 시작은 숨김
            content.SetActive(false);

            Undo.CollapseUndoOperations(undoGroup);

            string msg = $"TooltipPopup을 Canvas 아래에 생성했습니다.\n" +
                         (wiredOpen
                            ? "✅ ButtonTooltip의 OnClick → Show 자동 연결 완료"
                            : "⚠️ ButtonTooltip을 찾지 못했습니다. 수동으로 OnClick에 TooltipPopup.Show를 연결하세요.");
            EditorUtility.DisplayDialog("Tooltip 팝업 생성 완료", msg, "OK");
            Debug.Log($"[TooltipPopupGenerator] popup={rootGO.name}, autoWired={wiredOpen}");
        }

        // ── 헬퍼 ────────────────────────────────────────────────────

        static GameObject CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static GameObject EnsureChild(Transform parent, string name, bool addImage, bool addButton)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            if (addImage && go.GetComponent<Image>() == null) Undo.AddComponent<Image>(go);
            if (addButton && go.GetComponent<Button>() == null) Undo.AddComponent<Button>(go);
            return go;
        }

        static Transform EnsureTMPText(
            Transform parent, string name, string initialText,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            float fontSize, TextAlignmentOptions alignment,
            Color color, FontStyles style)
        {
            var existing = parent.Find(name);
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
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                tmp = Undo.AddComponent<TextMeshProUGUI>(go);
            }

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Undo.RecordObject(tmp, "Configure Text");
            if (!string.IsNullOrEmpty(initialText)) tmp.text = initialText;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);

            return rt;
        }

        /// <summary>
        /// Button.onClick에 popup의 메서드를 persistent listener로 추가한다.
        /// 같은 메서드가 이미 등록돼 있으면 중복 추가하지 않음.
        /// </summary>
        static void WirePersistentClick(Button btn, TooltipPopup popup, string methodName)
        {
            if (btn == null || popup == null) return;

            // 중복 방지: 같은 (target, method) 조합이 이미 있으면 skip
            int count = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (btn.onClick.GetPersistentTarget(i) == popup &&
                    btn.onClick.GetPersistentMethodName(i) == methodName)
                    return;
            }

            UnityAction action = (UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityAction), popup, methodName);
            UnityEventTools.AddPersistentListener(btn.onClick, action);

            EditorUtility.SetDirty(btn);
        }
    }
}
#endif
