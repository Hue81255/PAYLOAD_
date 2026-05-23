#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TraitTree.EditorTools
{
    /// <summary>
    /// 선택된 RectTransform을 부모에 꽉 차게 stretch-fill로 맞춘다.
    /// Inspector에서 Anchor Presets → Alt+Shift+우하단 클릭과 동일한 효과.
    /// </summary>
    public static class RectTransformUtilityMenu
    {
        [MenuItem("PAYLOAD/Stretch Selected RectTransform to Fill Parent")]
        public static void StretchFill()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("선택 없음",
                    "Hierarchy에서 stretch로 맞출 RectTransform이 있는 GameObject를 선택하세요.",
                    "OK");
                return;
            }

            var rt = selected.transform as RectTransform;
            if (rt == null)
            {
                EditorUtility.DisplayDialog("RectTransform 아님",
                    $"'{selected.name}'은 RectTransform이 아닙니다. (일반 Transform)",
                    "OK");
                return;
            }

            Undo.RecordObject(rt, "Stretch RectTransform to Fill Parent");

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            EditorUtility.SetDirty(rt);
            if (PrefabUtility.IsPartOfPrefabInstance(rt))
                PrefabUtility.RecordPrefabInstancePropertyModifications(rt);

            Debug.Log($"[RectTransformUtility] '{selected.name}' → 부모({(rt.parent != null ? rt.parent.name : "없음")})에 stretch-fill 적용");
        }
    }
}
#endif
