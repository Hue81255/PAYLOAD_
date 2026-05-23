#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TraitTree.EditorTools
{
    public static class TraitTreeTestBootstrapMenu
    {
        [MenuItem("PAYLOAD/Add Test Bootstrap to Current Scene")]
        public static void Add()
        {
            var existing = Object.FindObjectOfType<TraitTreeTestBootstrap>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog(
                    "이미 존재",
                    $"현재 씬에 TraitTreeTestBootstrap이 이미 있습니다.\n({existing.gameObject.name})",
                    "OK");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            var go = new GameObject("TraitTreeTestBootstrap");
            Undo.RegisterCreatedObjectUndo(go, "Add TraitTreeTestBootstrap");
            Undo.AddComponent<TraitTreeTestBootstrap>(go);

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            EditorUtility.DisplayDialog(
                "추가 완료",
                "TraitTreeTestBootstrap을 현재 씬에 추가했습니다.\n\n" +
                "Play 진입 시 PlayerStats/EvolutionManager/InfectionEngine이 씬에 없으면 자동 생성됩니다.\n" +
                "정식 빌드 전에 이 GameObject를 삭제하세요.",
                "OK");
        }
    }
}
#endif
