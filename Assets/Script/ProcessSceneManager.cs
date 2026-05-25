using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
<<<<<<< Updated upstream
/// Process씬(업그레이드 화면) 전용 매니저.
/// - Start(): 저장 파일에서 PlayerStats/EvolutionManager 복원
/// - BackToGame(): 업그레이드 내용 저장 후 New main 복귀
/// - BackToSelect(): 저장 삭제 후 select씬으로
=======
/// Process 씬 (업그레이드 화면). 확인 버튼으로 New main 으로 복귀한다.
>>>>>>> Stashed changes
/// </summary>
public class ProcessSceneManager : MonoBehaviour
{
    public static ProcessSceneManager Instance;

<<<<<<< Updated upstream
    [Tooltip("돌아갈 메인 씬 이름. Build Settings에 등록돼 있어야 한다.")]
    public string mainSceneName = "New main";

=======
>>>>>>> Stashed changes
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

<<<<<<< Updated upstream
    void Start()
    {
        Time.timeScale = 1f;

        // 저장 파일에서 PlayerStats(코인)와 EvolutionManager(레벨) 복원
        // GameManager가 없어도 null-safe로 처리됨
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
            SaveManager.Instance.Load();
    }

    // 확인 버튼: 업그레이드 저장 후 게임으로 복귀
    public void BackToGame()
    {
        SaveManager.Instance?.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
    }

    // 처음부터 버튼: 세이브 삭제 후 바이러스 선택 화면으로
    public void BackToSelect()
    {
        SaveManager.Instance?.DeleteSave();
        GameFlowData.IsNewGame = true;
=======
    // 확인/닫기 버튼 OnClick 에 연결
    public void BackToGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("New main");
    }

    // 처음부터 다시
    public void BackToSelect()
    {
>>>>>>> Stashed changes
        Time.timeScale = 1f;
        SceneManager.LoadScene("select");
    }
}
