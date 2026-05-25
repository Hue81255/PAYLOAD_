using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("게임 상태")]
<<<<<<< Updated upstream
    public bool isGameStarted = false;
    public bool isGameOver    = false;
    public bool isGameClear   = false;
=======
    public bool isGameStarted  = false;
    public bool isGameOver     = false;
    public bool isGameClear    = false;
>>>>>>> Stashed changes

    [Header("구역 설정")]
    public int totalRegions    = 9;
    public int infectedRegions = 0;

    [Header("UI 패널 연결")]
    public GameObject gameOverPanel;
    public GameObject gameClearPanel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (gameOverPanel)  gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

        // 한 프레임 대기: RegionDataLoader.Start()가 InfectionEngine.regions를
        // 채운 뒤 실행해야 감염 상태 복원 / 첫 감염이 정상 동작함
        StartCoroutine(InitGame());
    }

    IEnumerator InitGame()
    {
        yield return null; // 모든 Start() 완료 대기

        if (GameFlowData.IsNewGame)
        {
<<<<<<< Updated upstream
            // select씬에서 바이러스 선택 후 최초 진입
            GameFlowData.IsNewGame = false;
            MalwareSelectionManager.Instance?.ApplyNewGame(GameFlowData.SelectedMalwareType);
            StartGame();

            // 중구를 시작 지역으로 자동 감염 후 전파 시작
            RegionData start = RegionDataLoader.Instance?.GetRegionById("JUNG_GU");
            if (start != null) ConfirmStartInfection(start);
            else SpreadManager.Instance?.StartSpread();
        }
        else
        {
            // Process씬 업그레이드 후 복귀 → 저장 파일로 상태 복원
            SaveManager.CurrentSlot = GameFlowData.SelectedSlot;
            if (SaveManager.Instance == null || !SaveManager.Instance.Load())
            {
                // 저장 파일 없으면 select씬으로 이동
                GameFlowData.IsNewGame = true;
                SceneManager.LoadScene("select");
=======
            // select 씬에서 바이러스 선택 후 처음 진입
            GameFlowData.IsNewGame = false;
            StartGame();
            MalwareSelectionManager.Instance?.ApplyNewGame(GameFlowData.SelectedMalwareType);

            // 중구를 시작 지역으로 자동 감염
            RegionData start = RegionDataLoader.Instance?.GetRegionById("JUNG_GU");
            if (start != null)
                ConfirmStartInfection(start);
            else
                SpreadManager.Instance?.StartSpread();
        }
        else
        {
            // Process 씬에서 업그레이드 후 복귀 → 저장 데이터 복원
            bool loaded = SaveManager.Instance != null && SaveManager.Instance.Load();
            if (!loaded)
            {
                // 로드 실패 시 새 게임으로 폴백
                StartGame();
                SpreadManager.Instance?.StartSpread();
>>>>>>> Stashed changes
            }
        }
    }

    void Update()
    {
        if (!isGameStarted || isGameOver || isGameClear) return;
        CheckGameClear();
    }

    void CheckGameClear()
    {
        if (infectedRegions < totalRegions) return;

        isGameClear   = true;
        isGameStarted = false;
        Time.timeScale = 0f;

<<<<<<< Updated upstream
        SaveManager.Instance?.DeleteSave();

        if (gameClearPanel != null)
=======
        if (gameClearPanel)
>>>>>>> Stashed changes
        {
            gameClearPanel.transform.parent.gameObject.SetActive(true);
            gameClearPanel.SetActive(true);
        }
        Debug.Log("게임 클리어!");
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver    = true;
        isGameStarted = false;
        Time.timeScale = 0f;

<<<<<<< Updated upstream
        SaveManager.Instance?.DeleteSave();

        if (gameOverPanel != null)
=======
        if (gameOverPanel)
>>>>>>> Stashed changes
        {
            gameOverPanel.transform.parent.gameObject.SetActive(true);
            gameOverPanel.SetActive(true);
        }
<<<<<<< Updated upstream
=======
        Debug.Log("게임 오버!");
>>>>>>> Stashed changes
    }

    public void StartGame()
    {
<<<<<<< Updated upstream
        isGameStarted   = true;
        isGameOver      = false;
        isGameClear     = false;
=======
        isGameStarted  = true;
        isGameOver     = false;
        isGameClear    = false;
>>>>>>> Stashed changes
        infectedRegions = 0;
        Time.timeScale  = 1f;

        if (gameOverPanel)  gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

        CureManager.Instance?.ResetCure();
        WhiteHackerManager.Instance?.ResetAI();
        RegionAdjacencyManager.Instance?.ResetAll();
        EvolutionManager.Instance?.ResetLevels();
        SpreadManager.Instance?.StopSpread();
    }

    // 첫 지역 감염 + 자동 전파 시작
    public void ConfirmStartInfection(RegionData region)
    {
        if (region == null) return;
        region.isInfected = true;
        OnRegionInfected();
        GlobalEventManager.CallHackSuccess(region.id, region.reward);
        UIManager.Instance?.ShowWarning($"{region.name} 지역에서 바이러스 전파 시작!");
        SpreadManager.Instance?.StartSpread();
<<<<<<< Updated upstream
    }

    // 업그레이드 버튼 → Process씬 (이동 전 저장)
    public void GoToProcess()
    {
        SaveManager.Instance?.Save();
        Time.timeScale = 0f;
        SceneManager.LoadScene("Process");
    }

    // 다시하기 → select씬
    public void RestartGame()
    {
        SaveManager.Instance?.DeleteSave();
        GameFlowData.IsNewGame = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("select");
    }

    // 메인 메뉴로
    public void GoToMainMenu()
    {
        SaveManager.Instance?.DeleteSave();
        GameFlowData.IsNewGame = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScreen");
    }

    // SaveManager.Load() 복원 완료 후 호출 (리셋 없이 게임 재개)
    public void LoadAndStartGame()
    {
        isGameStarted = true;
        isGameOver    = false;
        isGameClear   = false;
        Time.timeScale = 1f;

        if (gameOverPanel)  gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

        UIManager.Instance?.ResetUI();
        SpreadManager.Instance?.StartSpread();
=======
>>>>>>> Stashed changes
    }

    public void OnRegionInfected()
    {
        infectedRegions++;
        CureManager.Instance?.OnRegionInfected();
    }

    public void OnRegionCured()
    {
        infectedRegions = Mathf.Max(0, infectedRegions - 1);
    }

    // 업그레이드 버튼 → Process 씬 (진입 전 저장)
    public void GoToProcess()
    {
        SaveManager.Instance?.Save();
        Time.timeScale = 0f;
        SceneManager.LoadScene("Process");
    }

    // 다시하기 → select 씬
    public void RestartGame()
    {
        SaveManager.Instance?.DeleteSave();
        GameFlowData.IsNewGame = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("select");
    }

    // 메인 메뉴로
    public void GoToMainMenu()
    {
        SaveManager.Instance?.DeleteSave();
        GameFlowData.IsNewGame = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScreen");
    }
}
