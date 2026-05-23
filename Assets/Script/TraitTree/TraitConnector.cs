using UnityEngine;
using UnityEngine.UI;

namespace TraitTree
{
    /// <summary>
    /// 두 TraitNodeUI 사이를 잇는 선 + 화살촉. 자식으로 Line/Wing1/Wing2 Image 3장을 코드로 생성.
    /// 별도 프리팹/스프라이트 불필요(기본 흰 사각형 이미지를 회전/스케일).
    /// </summary>
    public class TraitConnector : MonoBehaviour
    {
        public TraitNodeUI from;
        public TraitNodeUI to;

        [Header("Line")]
        public float thickness = 4f;

        [Header("Arrow Head")]
        public bool  arrowHeadEnabled = true;
        public float arrowHeadLength  = 12f;
        public float arrowHeadAngle   = 30f;
        public float arrowHeadInset   = 8f;   // to 노드 중심으로부터 화살촉 끝까지 떼어놓는 거리

        [Header("Colors")]
        public Color lockedColor   = new Color(1f, 1f, 1f, 0.15f);
        public Color readyColor    = new Color(0.30f, 0.55f, 1f, 0.9f);
        public Color unlockedColor = new Color(1f, 0.8f, 0.2f, 0.9f);

        RectTransform lineRT, wing1RT, wing2RT;
        Image lineImg, wing1Img, wing2Img;

        void Awake()
        {
            // 루트는 부모 전체에 펴서 자식 좌표가 부모 중심 기준이 되도록
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            EnsureGraphics();
        }

        void EnsureGraphics()
        {
            if (lineRT == null) CreatePart("Line", out lineRT, out lineImg);
            if (arrowHeadEnabled && wing1RT == null)
            {
                CreatePart("Wing1", out wing1RT, out wing1Img);
                CreatePart("Wing2", out wing2RT, out wing2Img);
            }
        }

        void CreatePart(string partName, out RectTransform rOut, out Image iOut)
        {
            var go = new GameObject(partName, typeof(RectTransform), typeof(Image));
            var r = (RectTransform)go.transform;
            r.SetParent(transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            rOut = r;
            iOut = img;
        }

        public void Bind(TraitNodeUI fromNode, TraitNodeUI toNode)
        {
            from = fromNode;
            to   = toNode;
        }

        public void UpdateLine()
        {
            if (from == null || to == null) return;
            EnsureGraphics();

            var parentRT = (RectTransform)transform;
            Vector2 a = WorldCenterToLocal((RectTransform)from.transform, parentRT);
            Vector2 b = WorldCenterToLocal((RectTransform)to.transform,   parentRT);
            Vector2 diff = b - a;
            float dist = diff.magnitude;
            if (dist < 0.001f) return;

            float angleDeg = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            Vector2 dir = diff / dist;

            float effectiveInset = arrowHeadEnabled ? Mathf.Min(arrowHeadInset, dist * 0.4f) : 0f;
            Vector2 tipPoint = b - dir * effectiveInset;
            float lineLen = (tipPoint - a).magnitude;
            Vector2 lineMid = (a + tipPoint) * 0.5f;

            // 본선
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.anchoredPosition = lineMid;
            lineRT.sizeDelta = new Vector2(lineLen, thickness);
            lineRT.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

            // 화살촉 (두 날개, 피벗을 왼쪽 끝=tip에 두고 뒤로 벌어지게)
            if (arrowHeadEnabled && wing1RT != null && wing2RT != null)
            {
                wing1RT.gameObject.SetActive(true);
                wing2RT.gameObject.SetActive(true);

                ConfigureWing(wing1RT, tipPoint, angleDeg + 180f - arrowHeadAngle);
                ConfigureWing(wing2RT, tipPoint, angleDeg + 180f + arrowHeadAngle);
            }
            else
            {
                if (wing1RT != null) wing1RT.gameObject.SetActive(false);
                if (wing2RT != null) wing2RT.gameObject.SetActive(false);
            }
        }

        void ConfigureWing(RectTransform wing, Vector2 tipPoint, float zAngle)
        {
            wing.pivot = new Vector2(0f, 0.5f);
            wing.anchoredPosition = tipPoint;
            wing.sizeDelta = new Vector2(arrowHeadLength, thickness);
            wing.localRotation = Quaternion.Euler(0f, 0f, zAngle);
        }

        public void Refresh()
        {
            EnsureGraphics();
            Color col = ComputeColor();
            if (lineImg  != null) lineImg.color  = col;
            if (wing1Img != null) wing1Img.color = col;
            if (wing2Img != null) wing2Img.color = col;
        }

        Color ComputeColor()
        {
            if (from == null || from.node == null || TraitTreeManager.Instance == null)
                return lockedColor;
            bool fromUnlocked = TraitTreeManager.Instance.IsUnlocked(from.node);
            bool toUnlocked   = to != null && to.node != null
                                && TraitTreeManager.Instance.IsUnlocked(to.node);
            if (fromUnlocked && toUnlocked) return unlockedColor;
            if (fromUnlocked)               return readyColor;
            return lockedColor;
        }

        static Vector2 WorldCenterToLocal(RectTransform target, RectTransform parent)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            return parent.InverseTransformPoint(worldCenter);
        }
    }
}
