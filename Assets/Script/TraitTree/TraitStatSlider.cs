using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TraitTree
{
    /// <summary>
    /// 스탯 슬라이더 + 어두운 미리보기 오버레이.
    ///
    /// 사용 방식 2가지:
    ///   1) 기존 Slider 하나만 재활용 → previewSlider를 비워두면 actualSlider의
    ///      Fill Area 안에 Image 오버레이를 자동 생성해서 미리보기를 표시.
    ///   2) Slider 두 개를 직접 겹쳐서 사용 → actualSlider/previewSlider 둘 다 지정.
    /// </summary>
    public class TraitStatSlider : MonoBehaviour
    {
        [Tooltip("실제 스탯을 표시하는 Slider (필수)")]
        public Slider actualSlider;

        [Tooltip("뒤에 깔릴 미리보기 Slider. 비워두면 Image 오버레이를 자동 생성한다.")]
        public Slider previewSlider;

        public TMP_Text valueText;
        public int  maxStat = 100;

        int currentActual;
        Image autoPreviewImage; // previewSlider가 null일 때 자동 생성되는 오버레이

        // ── 외부 API ───────────────────────────────────────────────

        public void SetActual(int value)
        {
            currentActual = value;
            float n = Normalize(value);

            if (actualSlider  != null) actualSlider.value  = n;

            if (previewSlider != null) previewSlider.value = n;
            else                       SetAutoPreviewFill(n);

            if (valueText != null) valueText.text = value.ToString();
        }

        public void ShowPreview(int delta)
        {
            float n = Normalize(currentActual + delta);
            if (previewSlider != null) previewSlider.value = n;
            else                       SetAutoPreviewFill(n);
        }

        public void ClearPreview()
        {
            float n = Normalize(currentActual);
            if (previewSlider != null && actualSlider != null)
                previewSlider.value = actualSlider.value;
            else
                SetAutoPreviewFill(n);
        }

        /// <summary>
        /// 두 슬라이더의 Fill Image 색상을 코드에서 지정한다.
        /// 자동 오버레이가 있으면 함께 색을 입힌다.
        /// </summary>
        public void ApplyFillColors(Color actualColor, Color previewColor)
        {
            if (actualSlider != null && actualSlider.fillRect != null)
            {
                var img = actualSlider.fillRect.GetComponent<Image>();
                if (img != null) img.color = actualColor;
            }
            if (previewSlider != null && previewSlider.fillRect != null)
            {
                var img = previewSlider.fillRect.GetComponent<Image>();
                if (img != null) img.color = previewColor;
            }
            else
            {
                EnsureAutoPreview();
                if (autoPreviewImage != null) autoPreviewImage.color = previewColor;
            }
        }

        // ── 내부 ───────────────────────────────────────────────────

        float Normalize(int value) =>
            Mathf.Clamp01(value / (float)Mathf.Max(1, maxStat));

        void SetAutoPreviewFill(float n)
        {
            EnsureAutoPreview();
            if (autoPreviewImage != null) autoPreviewImage.fillAmount = n;
        }

        /// <summary>
        /// actualSlider의 Fill Area에 미리보기용 Image 한 장을 깔아둔다.
        /// Image.type = Filled로 두고 fillAmount만 갱신해서 슬라이더처럼 동작.
        /// 실제 Fill 뒤(sibling index 0)에 배치해 actual이 앞에 덮이도록 한다.
        /// </summary>
        void EnsureAutoPreview()
        {
            if (autoPreviewImage != null) return;
            if (actualSlider == null || actualSlider.fillRect == null) return;

            var fillRect   = actualSlider.fillRect;
            var fillParent = fillRect.parent; // 보통 "Fill Area"
            if (fillParent == null) return;

            var go = new GameObject("AutoPreviewFill",
                                    typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(fillParent, false);
            rt.SetSiblingIndex(0); // 실제 Fill보다 뒤에 그려지도록

            // Fill Area 전체 영역을 덮음 → fillAmount로 길이 제어
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.type        = Image.Type.Filled;
            img.fillMethod  = Image.FillMethod.Horizontal;
            img.fillOrigin  = (int)Image.OriginHorizontal.Left;
            img.fillAmount  = 0f;

            // 기존 Fill의 sprite/material을 그대로 따라가서 시각 톤 유지
            var fillImg = fillRect.GetComponent<Image>();
            if (fillImg != null)
            {
                if (fillImg.sprite != null) img.sprite = fillImg.sprite;
                img.material = fillImg.material;
            }

            autoPreviewImage = img;
        }
    }
}
