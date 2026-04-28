using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("게임 상태")]
    public bool isGameStarted = false;
    public bool isGameOver = false;
    public bool isGameClear = false;

    [Header("구역 설정")]
    public int totalRegions = 9;
    public int infectedRegions = 0;

    [Header("UI 패널 연결")]
    public GameObject mainMenuPanel;
    public GameObject gameOverPanel;
    public GameObject gameClearPanel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        if (!isGameStarted || isGameOver || isGameClear) return;
        CheckGameClear();
    }

    void CheckGameClear()
    {
        if (infectedRegions >= totalRegions)
        {
            isGameClear = true;
            isGameStarted = false;
            Time.timeScale = 0f;

            // 부모까지 강제 활성화
            gameClearPanel.transform.parent.gameObject.SetActive(true);
            gameClearPanel.SetActive(true);
            Debug.Log("🎉 게임 클리어!");
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        isGameStarted = false;
        Time.timeScale = 0f;

        // 메인메뉴 패널 숨기기
        mainMenuPanel.SetActive(false);

        gameOverPanel.transform.parent.gameObject.SetActive(true);
        gameOverPanel.SetActive(true);
        Debug.Log($"💀 발각! 패널활성화: {gameOverPanel.name}");
    }


    public void StartGame()
    {
        isGameStarted = true;
        isGameOver = false;
        isGameClear = false;
        infectedRegions = 0;
        Time.timeScale = 1f;

        mainMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameClearPanel.SetActive(false);

        if (CureManager.Instance != null)
            CureManager.Instance.ResetCure();
        if (WhiteHackerManager.Instance != null)
            WhiteHackerManager.Instance.ResetAI();

        Debug.Log("🎮 게임 시작!");
    }

    public void RestartGame()
    {
        StartGame();
        if (UIManager.Instance != null)
            UIManager.Instance.ResetUI();
    }

    public void GoToMainMenu()
    {
        isGameStarted = false;
        isGameOver = false;
        isGameClear = false;
        infectedRegions = 0;
        Time.timeScale = 1f;

        if (CureManager.Instance != null)
            CureManager.Instance.ResetCure();
        if (WhiteHackerManager.Instance != null)
            WhiteHackerManager.Instance.ResetAI();

        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        gameClearPanel.SetActive(false);
    }

    public void OnRegionInfected()
    {
        infectedRegions++;
        if (CureManager.Instance != null)
            CureManager.Instance.OnRegionInfected();
    }

    public void OnRegionCured()
    {
        infectedRegions = Mathf.Max(0, infectedRegions - 1);
    }
}
