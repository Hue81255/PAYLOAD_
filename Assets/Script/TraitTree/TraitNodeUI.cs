using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TraitTree
{
    [RequireComponent(typeof(RectTransform))]
    public class TraitNodeUI : MonoBehaviour, IPointerClickHandler
    {
        public TraitNode node;

        [Header("UI Refs")]
        public Image    background;
        public Image    border;
        public TMP_Text label;

        [Header("State Colors")]
        public Color lockedColor    = new Color(0.18f, 0.18f, 0.22f, 1f);
        public Color availableColor = new Color(0.30f, 0.55f, 0.30f, 1f);
        public Color nextColor      = new Color(0.30f, 0.55f, 1.00f, 1f);
        public Color unlockedColor  = new Color(1.00f, 0.80f, 0.20f, 1f);
        public Color borderSelected = Color.white;
        public Color borderHidden   = new Color(1f, 1f, 1f, 0f);

        [Header("Double Click")]
        [Tooltip("이 시간(초) 안에 두 번째 클릭이 들어오면 더블클릭으로 간주한다. Unity의 eventData.clickCount보다 안정적.")]
        public float doubleClickWindow = 0.4f;

        TraitTreeView view;
        float lastClickTime = -10f;

        public void Bind(TraitTreeView v)
        {
            view = v;
            if (label != null && node != null) label.text = node.displayName;
            Refresh(false, false);
        }

        public void Refresh(bool selected, bool isNextOfSelected)
        {
            if (TraitTreeManager.Instance == null || node == null) return;

            bool unlocked  = TraitTreeManager.Instance.IsUnlocked(node);
            bool available = TraitTreeManager.Instance.IsAvailable(node);

            if (background != null)
            {
                if (unlocked)              background.color = unlockedColor;
                else if (isNextOfSelected) background.color = nextColor;
                else if (available)        background.color = availableColor;
                else                       background.color = lockedColor;
            }
            if (border != null)
                border.color = selected ? borderSelected : borderHidden;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (view == null) return;

            // Unity의 clickCount는 클릭 간격이 짧아야만 2가 되므로 시간 기반 판단을 병행한다.
            float now = Time.unscaledTime;
            bool timeBasedDouble = (now - lastClickTime) <= doubleClickWindow;
            lastClickTime = now;

            bool isDoubleClick = eventData.clickCount >= 2 || timeBasedDouble;

            if (view.debugLogs)
                Debug.Log($"[TraitNodeUI:{(node != null ? node.name : "?")}] click " +
                          $"unityCount={eventData.clickCount}, timeBasedDouble={timeBasedDouble} → " +
                          $"{(isDoubleClick ? "DOUBLE" : "SINGLE")}");

            if (isDoubleClick) view.OnNodeDoubleClick(this);
            else               view.OnNodeClick(this);
        }
    }
}
