using UnityEngine;
using UnityEngine.SceneManagement;

public class ProcessSceneManager : MonoBehaviour
{
    public static ProcessSceneManager Instance;

    [Tooltip("돌아갈 메인 씬 이름. Build Settings에 등록돼 있어야 한다.")]
    public string mainSceneName = "New main";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Time.timeScale = 1f;

        // 저장 파일에서 PlayerStats(코인)와 EvolutionManager(레벨) 복원
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
            SaveManager.Instance.Load();

        // 어느 지역을 편집 중인지 TraitTreeManager에 알림
        TraitTree.TraitTreeManager.Instance?.SetCurrentRegion(GameFlowData.SelectedRegionId);
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("select");
    }
}
