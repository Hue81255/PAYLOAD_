using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;

public class EventSystemCleaner
{
    [MenuItem("Tools/EventSystem 중복 제거 (현재 씬)")]
    static void RemoveDuplicates()
    {
        EventSystem[] all = Object.FindObjectsOfType<EventSystem>(true);
        if (all.Length <= 1)
        {
            EditorUtility.DisplayDialog("확인", "EventSystem이 1개입니다. 문제 없습니다.", "확인");
            return;
        }

        // 첫 번째는 남기고 나머지 삭제
        for (int i = 1; i < all.Length; i++)
        {
            Debug.Log($"[EventSystemCleaner] 삭제: {all[i].gameObject.name}");
            Undo.DestroyObjectImmediate(all[i].gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("완료",
            $"중복 EventSystem {all.Length - 1}개를 삭제했습니다.\nCtrl+S 로 씬을 저장하세요.", "확인");
    }
}
