using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Process 씬에서의 씬 전환을 담당한다.
/// BackToGame() → "New main" 씬으로 복귀.
/// </summary>
public class ProcessSceneManager : MonoBehaviour
{
    [Tooltip("뒤로 갈 때 로드할 메인 씬 이름. Build Settings에 등록돼 있어야 한다.")]
    public string mainSceneName = "New main";

    /// <summary>
    /// ButtonBack.onClick에 연결되는 콜백.
    /// </summary>
    public void BackToGame()
    {
        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogError("[ProcessSceneManager] mainSceneName이 비어있습니다.");
            return;
        }
        SceneManager.LoadScene(mainSceneName);
    }
}
