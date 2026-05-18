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

    [Header("메인 메뉴 버튼")]
    [Tooltip("저장 파일이 있을 때만 활성화되는 '계속하기' 버튼")]
    public UnityEngine.UI.Button continueButton;

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
            isGameClear   = true;
            isGameStarted = false;
            Time.timeScale = 0f;

            SaveManager.Instance?.DeleteSave(); // 클리어 시 세이브 삭제

            gameClearPanel.transform.parent.gameObject.SetActive(true);
            gameClearPanel.SetActive(true);
            Debug.Log("🎉 게임 클리어!");
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver    = true;
        isGameStarted = false;
        Time.timeScale = 0f;

        SaveManager.Instance?.DeleteSave(); // 게임 오버 시 세이브 삭제

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
        if (RegionAdjacencyManager.Instance != null)
            RegionAdjacencyManager.Instance.ResetAll();
        if (EvolutionManager.Instance != null)
            EvolutionManager.Instance.ResetLevels();

        Debug.Log("🎮 게임 시작!");
    }

    // UI 버튼(시작 / 다시하기)이 호출하는 진입점 – 악성코드 선택 패널을 먼저 표시
    public void OnStartButtonPressed()
    {
        if (MalwareSelectionManager.Instance != null)
            MalwareSelectionManager.Instance.ShowSelectionPanel();
        else
            StartGame();
    }

    public void RestartGame()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ResetUI();
        OnStartButtonPressed();
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

        // 저장 파일이 있을 때만 '계속하기' 버튼 표시
        if (continueButton != null)
            continueButton.gameObject.SetActive(SaveManager.Instance != null && SaveManager.Instance.HasSave());
    }

    // '계속하기' 버튼 콜백 – 저장 파일을 로드하고 게임 재개
    public void ContinueGame()
    {
        SaveManager.Instance?.Load();
    }

    // SaveManager.Load()가 모든 상태를 복원한 뒤 마지막으로 호출
    // StartGame()과 달리 매니저를 리셋하지 않는다
    public void LoadAndStartGame()
    {
        isGameStarted = true;
        isGameOver    = false;
        isGameClear   = false;
        Time.timeScale = 1f;

        mainMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameClearPanel.SetActive(false);

        UIManager.Instance?.ResetUI();
        Debug.Log("💾 게임 재개!");
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
