using UnityEngine;
using UnityEngine.UI;

public class CureUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider cureSlider;
    public Text   cureText;
    public Image  fillImage;

    void Update()
    {
        if (CureManager.Instance == null) return;
        float progress = CureManager.Instance.cureProgress;

        if (cureSlider != null) cureSlider.value = progress / 100f;
        if (cureText   != null) cureText.text    = $"발각도 : {Mathf.FloorToInt(progress)}%";

        if (fillImage != null)
        {
            if      (progress < 30f) fillImage.color = Color.green;
            else if (progress < 60f) fillImage.color = Color.yellow;
            else if (progress < 90f) fillImage.color = new Color(1f, 0.5f, 0f);
            else                     fillImage.color = Color.red;
        }
    }
}
