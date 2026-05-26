using UnityEditor;

[InitializeOnLoad]
public static class PlayModePreviewFix
{
    static PlayModePreviewFix()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Play 진입 직전에 선택 해제 — PreviewWindow SerializedObject dispose 버그 방지
        if (state == PlayModeStateChange.ExitingEditMode)
            Selection.activeObject = null;
    }
}
