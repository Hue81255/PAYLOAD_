#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TraitTree.EditorTools
{
    /// <summary>
    /// ProcessSceneManager GameObject가 없으면 생성하고,
    /// 씬 안의 ButtonBack OnClick → ProcessSceneManager.BackToGame을 자동 연결한다.
    /// 메뉴: PAYLOAD → Wire ButtonBack to ProcessSceneManager
    /// </summary>
    public static class ProcessSceneManagerSetup
    {
        [MenuItem("PAYLOAD/Wire ButtonBack to ProcessSceneManager")]
        public static void Setup()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Wire ButtonBack");

            // 1) ProcessSceneManager 확보
            var manager = Object.FindObjectOfType<ProcessSceneManager>();
            bool managerCreated = false;
            if (manager == null)
            {
                var go = new GameObject("ProcessSceneManager");
                Undo.RegisterCreatedObjectUndo(go, "Create ProcessSceneManager");
                manager = Undo.AddComponent<ProcessSceneManager>(go);
                managerCreated = true;
            }

            // 2) ButtonBack 찾기
            var buttonGO = GameObject.Find("ButtonBack");
            if (buttonGO == null)
            {
                EditorUtility.DisplayDialog("ButtonBack 없음",
                    "현재 씬에서 'ButtonBack' GameObject를 찾을 수 없습니다.\n" +
                    "Process 씬을 열고 다시 실행하세요.", "OK");
                return;
            }
            var btn = buttonGO.GetComponent<Button>();
            if (btn == null)
            {
                EditorUtility.DisplayDialog("Button 컴포넌트 없음",
                    $"'{buttonGO.name}'에 Button 컴포넌트가 없습니다.", "OK");
                return;
            }

            // 3) onClick → BackToGame persistent listener 추가 (중복 방지)
            bool alreadyWired = false;
            int count = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (btn.onClick.GetPersistentTarget(i) == manager &&
                    btn.onClick.GetPersistentMethodName(i) == nameof(ProcessSceneManager.BackToGame))
                {
                    alreadyWired = true;
                    break;
                }
            }
            if (!alreadyWired)
            {
                UnityAction action = (UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityAction), manager, nameof(ProcessSceneManager.BackToGame));
                UnityEventTools.AddPersistentListener(btn.onClick, action);
                EditorUtility.SetDirty(btn);
            }

            // 4) "New main" 씬이 Build Settings에 있는지 확인
            bool inBuildSettings = IsSceneInBuildSettings(manager.mainSceneName);

            Undo.CollapseUndoOperations(undoGroup);

            // 결과 출력
            string status =
                (managerCreated ? "✅ ProcessSceneManager 자동 생성\n" : "✅ 기존 ProcessSceneManager 사용\n") +
                (alreadyWired   ? "✅ 이미 연결되어 있음 (중복 추가 안 함)\n" : "✅ ButtonBack.OnClick → BackToGame 연결 완료\n") +
                (inBuildSettings ? $"✅ '{manager.mainSceneName}' 씬이 Build Settings에 등록됨"
                                 : $"⚠️ '{manager.mainSceneName}' 씬이 Build Settings에 없음 — 추가 필요!");

            EditorUtility.DisplayDialog("ButtonBack 와이어링", status, "OK");
            Debug.Log($"[ProcessSceneManagerSetup] managerCreated={managerCreated}, " +
                      $"alreadyWired={alreadyWired}, inBuildSettings={inBuildSettings}");

            // Build Settings에 없으면 추가하시겠습니까?
            if (!inBuildSettings)
            {
                bool yes = EditorUtility.DisplayDialog("Build Settings에 추가?",
                    $"'{manager.mainSceneName}' 씬을 Build Settings에 자동으로 추가할까요?\n" +
                    "(없으면 런타임에 LoadScene이 실패합니다.)",
                    "추가", "나중에");
                if (yes) AddSceneToBuildSettings(manager.mainSceneName);
            }
        }

        static bool IsSceneInBuildSettings(string sceneName)
        {
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue;
                var name = System.IO.Path.GetFileNameWithoutExtension(s.path);
                if (name == sceneName) return true;
            }
            return false;
        }

        static void AddSceneToBuildSettings(string sceneName)
        {
            // 프로젝트에서 일치하는 씬 경로 찾기
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            string foundPath = null;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    foundPath = path;
                    break;
                }
            }
            if (foundPath == null)
            {
                EditorUtility.DisplayDialog("씬 파일 못 찾음",
                    $"'{sceneName}.unity' 파일이 프로젝트에 없습니다.\n" +
                    "수동으로 File → Build Settings에서 추가하세요.", "OK");
                return;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.Add(new EditorBuildSettingsScene(foundPath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[ProcessSceneManagerSetup] Build Settings에 추가됨: {foundPath}");
            EditorUtility.DisplayDialog("추가 완료",
                $"'{foundPath}'를 Build Settings에 추가했습니다.", "OK");
        }
    }
}
#endif
