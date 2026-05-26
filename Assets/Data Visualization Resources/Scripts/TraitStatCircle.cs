using UnityEngine;
using UnityEngine.UI;

namespace TraitTree
{
    public class TraitStatCircle : MonoBehaviour
    {
        [Tooltip("연결할 Wavecircle 스크립트")]
        public Wavecircle wavecircle;

        public int maxStat = 74;

        int currentActual;

        // ── 외부 API (TraitStatSlider와 동일) ──────────────────────

        public void SetActual(int value)
        {
            currentActual = value;
            if (wavecircle != null)
                wavecircle.SetPercent(Normalize(value));
        }

        public void ShowPreview(int delta)
        {
            if (wavecircle != null)
                wavecircle.SetPercent(Normalize(currentActual + delta));
        }

        public void ClearPreview()
        {
            if (wavecircle != null)
                wavecircle.SetPercent(Normalize(currentActual));
        }

        // ── 내부 ───────────────────────────────────────────────────

        float Normalize(int value) =>
            Mathf.Clamp(value / (float)Mathf.Max(1, maxStat) * 100f, 0f, 100f);
    }
}
