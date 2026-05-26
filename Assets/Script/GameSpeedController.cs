using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 멈춤 / 재생(1x) / 2배속 버튼을 처리한다.
/// 버튼 세 개의 OnClick()에 이 컴포넌트의 메서드를 연결한다.
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    [Header("버튼 하이라이트 (선택)")]
    public Image pauseButtonBg;
    public Image playButtonBg;
    public Image speed2xButtonBg;
    public Color activeColor   = new Color(1f, 0.8f, 0.2f, 1f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    void Start() => RefreshHighlight();

    public void Pause()
    {
        Time.timeScale = 0f;
        RefreshHighlight();
    }

    public void Play()
    {
        Time.timeScale = 1f;
        RefreshHighlight();
    }

    public void Speed2x()
    {
        Time.timeScale = 2f;
        RefreshHighlight();
    }

    void RefreshHighlight()
    {
        float s = Time.timeScale;
        SetBg(pauseButtonBg,  s == 0f);
        SetBg(playButtonBg,   s == 1f);
        SetBg(speed2xButtonBg, s == 2f);
    }

    void SetBg(Image img, bool active)
    {
        if (img != null) img.color = active ? activeColor : inactiveColor;
    }
}
