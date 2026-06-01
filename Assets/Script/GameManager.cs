using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("게임 상태")]
    public bool isGameStarted = false;
    public bool isGameOver    = false;
    public bool isGameClear   = false;

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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
                // 저장 파일 없음 (테스트 모드 등) → select씬으로 보내지 않고 새 게임으로 재개
                Debug.LogWarning("[GameManager] 저장 파일 없음 → 메인씬에서 새 게임으로 재시작");
                MalwareSelectionManager.Instance?.ApplyNewGame(GameFlowData.SelectedMalwareType);
                StartGame();
                RegionData start = RegionDataLoader.Instance?.GetRegionById("JUNG_GU");
                if (start != null) ConfirmStartInfection(start);
                else SpreadManager.Instance?.StartSpread();
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

        SaveManager.Instance?.DeleteSave();

        if (gameClearPanel != null)
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

        SaveManager.Instance?.DeleteSave();

        if (gameOverPanel != null)
        {
            gameOverPanel.transform.parent.gameObject.SetActive(true);
            gameOverPanel.SetActive(true);
        }
        Debug.Log("게임 오버!");
    }

    public void StartGame()
    {
        isGameStarted   = true;
        isGameOver      = false;
        isGameClear     = false;
        infectedRegions = 0;
        Time.timeScale  = 1f;

        if (gameOverPanel)  gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

        // 새 게임 시작 시 모든 구역 감염 상태를 명시적으로 초기화
        if (InfectionEngine.Instance != null)
            foreach (var r in InfectionEngine.Instance.regions)
            {
                r.isInfected       = false;
                r.defenseReduction = 0;
            }

        CureManager.Instance?.ResetCure();
        WhiteHackerManager.Instance?.ResetAI();
        RegionAdjacencyManager.Instance?.ResetAll();
        EvolutionManager.Instance?.ResetLevels();
        SpreadManager.Instance?.StopSpread();
        TraitTree.TraitTreeManager.ClearStaticData();
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

    // 업그레이드 버튼 → Process씬 (이동 전 저장)
    public void GoToProcess()
    {
        var region = UIManager.Instance?.GetSelectedRegion();
        GameFlowData.SelectedRegionId = region?.id ?? "";

        if (string.IsNullOrEmpty(GameFlowData.SelectedRegionId))
            UIManager.Instance?.ShowWarning("⚠️ 지역을 선택하지 않았습니다. 지역을 먼저 클릭하세요.");

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
    }
}
