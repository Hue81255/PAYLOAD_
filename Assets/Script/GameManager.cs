using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

        if (GameFlowData.IsNewGame)
        {
            MalwareSelectionManager.Instance?.ApplyNewGame(GameFlowData.SelectedMalwareType);
            StartGame();
        }
        else
        {
            SaveManager.CurrentSlot = GameFlowData.SelectedSlot;
            SaveManager.Instance?.Load();
        }
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

        SaveManager.Instance?.DeleteSave();

        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameOverPanel)
        {
            gameOverPanel.transform.parent.gameObject.SetActive(true);
            gameOverPanel.SetActive(true);
        }
        Debug.Log($"💀 발각! 패널활성화: {gameOverPanel?.name}");
    }


    public void StartGame()
    {
        isGameStarted = true;
        isGameOver = false;
        isGameClear = false;
        infectedRegions = 0;
        Time.timeScale = 1f;

        if (mainMenuPanel)  mainMenuPanel.SetActive(false);
        if (gameOverPanel)  gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

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
        Time.timeScale = 1f;
        SceneManager.LoadScene("select2");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScreen");
    }

    // SaveManager.Load()가 모든 상태를 복원한 뒤 마지막으로 호출
    // StartGame()과 달리 매니저를 리셋하지 않는다
    public void LoadAndStartGame()
    {
        isGameStarted = true;
        isGameOver    = false;
        isGameClear   = false;
        Time.timeScale = 1f;

        if (mainMenuPanel)  mainMenuPanel.SetActive(false);
        if (gameOverPanel)  gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

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
