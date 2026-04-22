using UnityEngine;
using UnityEngine.UI;

public class CureUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider cureSlider;
    public Text cureText;
    public Image fillImage;

    void Update()
    {
        float progress = CureManager.Instance.cureProgress;

        // 슬라이더 업데이트
        cureSlider.value = progress / 100f;

        // 텍스트 업데이트
        cureText.text = $"발각도 : {Mathf.FloorToInt(progress)}%";

        // 진행도에 따라 색상 변경
        if (progress < 30f)
            fillImage.color = Color.green;
        else if (progress < 60f)
            fillImage.color = Color.yellow;
        else if (progress < 90f)
            fillImage.color = new Color(1f, 0.5f, 0f); // 주황
        else
            fillImage.color = Color.red;
    }
}
