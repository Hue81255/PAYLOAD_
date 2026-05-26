using System.Collections;
using TMPro;
using UnityEngine;

public class NewsTickerUI : MonoBehaviour
{
    [Header("뉴스 표시 텍스트")]
    public TMP_Text newsText;

    [Header("뉴스 표시 시간(초)")]
    public float displayDuration = 8f;

    [Header("페이드 시간(초)")]
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private Coroutine displayCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        if (News.Instance != null)
            News.Instance.OnNewsPublished += ShowNews;
    }

    void OnDisable()
    {
        if (News.Instance != null)
            News.Instance.OnNewsPublished -= ShowNews;
    }

    void Start()
    {
        if (News.Instance != null)
        {
            News.Instance.OnNewsPublished -= ShowNews;
            News.Instance.OnNewsPublished += ShowNews;
        }
    }

    void OnDestroy()
    {
        if (News.Instance != null)
            News.Instance.OnNewsPublished -= ShowNews;
    }

    public void ShowNews(NewsItem item)
    {
        if (item == null) return;

        if (newsText != null)
            newsText.text = item.headline ?? "";

        if (displayCoroutine != null)
            StopCoroutine(displayCoroutine);
        displayCoroutine = StartCoroutine(DisplayRoutine());
    }

    IEnumerator DisplayRoutine()
    {
        yield return StartCoroutine(Fade(0f, 1f));
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return StartCoroutine(Fade(1f, 0f));
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
