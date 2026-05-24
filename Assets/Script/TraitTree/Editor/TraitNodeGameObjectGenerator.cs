#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TraitTree.EditorTools
{
    /// <summary>
    /// Hierarchy에서 선택된 TraitNodeUI 노드를 템플릿으로 삼아,
    /// Assets/Resources/Traits에 있는 모든 TraitNode 자산에 대해
    /// 대응되는 Node GameObject를 같은 부모 아래에 자동 생성한다.
    /// 메뉴: PAYLOAD → Generate Node GameObjects from Selected Template
    /// </summary>
    public static class TraitNodeGameObjectGenerator
    {
        // 카테고리 내부 트리 모양 위치 (자산 인덱스 0..7 → 로컬 좌표).
        //        N1 (root)
        //       /  \
        //      N2   N3
        //     /|    |\
        //    N4 N5 N6 N7
        //    |
        //    N8
        static readonly Vector2[] LocalTreePositions =
        {
            new Vector2(   0f,  60f), // N1 (root)
            new Vector2( -80f,  20f), // N2
            new Vector2(  80f,  20f), // N3
            new Vector2(-120f, -20f), // N4
            new Vector2( -40f, -20f), // N5
            new Vector2(  40f, -20f), // N6
            new Vector2( 120f, -20f), // N7
            new Vector2(-120f, -60f), // N8
        };

        const float CategoryYInf     =  200f;
        const float CategoryYComp    =    0f;
        const float CategoryYStealth = -200f;

        [MenuItem("PAYLOAD/Generate Node GameObjects from Selected Template")]
        public static void Generate()
        {
            var template = Selection.activeGameObject;
            if (template == null)
            {
                EditorUtility.DisplayDialog("템플릿 미선택",
                    "Hierarchy에서 템플릿 노드 GameObject(예: Node_Inf_1)를 선택한 뒤 다시 실행하세요.",
                    "OK");
                return;
            }

            var templateUI = template.GetComponent<TraitNodeUI>();
            if (templateUI == null)
            {
                EditorUtility.DisplayDialog("TraitNodeUI 없음",
                    $"'{template.name}'에 TraitNodeUI 컴포넌트가 없습니다.\n" +
                    "TraitNodeUI가 부착된 노드를 선택하세요.",
                    "OK");
                return;
            }

            var parent = template.transform.parent;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("부모 없음",
                    $"'{template.name}'이 씬 루트에 있습니다.\nImageEvolve 자식으로 옮긴 뒤 다시 시도하세요.",
                    "OK");
                return;
            }

            var assets = LoadAllAssetsSorted();
            if (assets.Count == 0)
            {
                EditorUtility.DisplayDialog("TraitNode 자산 없음",
                    "TraitNode 자산이 프로젝트에 없습니다.\n" +
                    "먼저 [PAYLOAD → Generate Sample Trait Tree (24 nodes)] 를 실행하세요.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Node GameObjects");

            int created = 0, updated = 0;
            var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(template);

            foreach (var asset in assets)
            {
                string goName = MakeNodeName(asset);

                // 같은 이름의 자식이 이미 있으면: TraitNodeUI.node만 재연결하고 GameObject 자체는 그대로 둠
                var existing = FindDirectChild(parent, goName);
                if (existing != null)
                {
                    var ui = existing.GetComponent<TraitNodeUI>();
                    if (ui != null && ui.node != asset)
                    {
                        Undo.RecordObject(ui, "Reassign TraitNode");
                        ui.node = asset;
                        if (PrefabUtility.IsPartOfPrefabInstance(ui))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
                        EditorUtility.SetDirty(ui);
                        updated++;
                    }
                    continue;
                }

                // 신규 생성: 프리팹 인스턴스면 PrefabUtility로, 아니면 일반 Instantiate
                GameObject go;
                if (prefabSource != null)
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, parent);
                else
                    go = (GameObject)Object.Instantiate(template, parent);

                Undo.RegisterCreatedObjectUndo(go, "Create Node GameObject");
                go.name = goName;

                var newUI = go.GetComponent<TraitNodeUI>();
                if (newUI != null)
                {
                    Undo.RecordObject(newUI, "Set TraitNode");
                    newUI.node = asset;
                    if (PrefabUtility.IsPartOfPrefabInstance(newUI))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(newUI);
                    EditorUtility.SetDirty(newUI);
                }

                ApplyTreePosition(go, asset);
                created++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            string msg = $"생성: {created}개\n업데이트: {updated}개\n부모: {parent.name}\n\n" +
                         "임시 트리 모양으로 배치했습니다. RectTransform을 조정해 디자인에 맞춰주세요.\n" +
                         "(템플릿 노드 위치는 건드리지 않았습니다.)";
            EditorUtility.DisplayDialog("노드 GameObject 생성 완료", msg, "OK");
            Debug.Log($"[NodeGameObjectGenerator] 생성 {created} + 업데이트 {updated} (부모: {parent.name})");
        }

        // ── 헬퍼 ────────────────────────────────────────────────────

        static string MakeNodeName(TraitNode asset)
        {
            // "Trait_Inf_1" → "Node_Inf_1"
            string n = asset.name;
            if (n.StartsWith("Trait_")) n = n.Substring("Trait_".Length);
            return "Node_" + n;
        }

        static List<TraitNode> LoadAllAssetsSorted()
        {
            var list = new List<TraitNode>();
            foreach (var guid in AssetDatabase.FindAssets("t:TraitNode"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var n = AssetDatabase.LoadAssetAtPath<TraitNode>(path);
                if (n != null) list.Add(n);
            }
            list.Sort((a, b) =>
            {
                int catCmp = ((int)a.category).CompareTo((int)b.category);
                if (catCmp != 0) return catCmp;
                return ExtractTrailingNumber(a.name).CompareTo(ExtractTrailingNumber(b.name));
            });
            return list;
        }

        static int ExtractTrailingNumber(string s)
        {
            int i = s.Length - 1;
            while (i >= 0 && char.IsDigit(s[i])) i--;
            string digits = s.Substring(i + 1);
            return int.TryParse(digits, out var n) ? n : 0;
        }

        static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
            }
            return null;
        }

        static void ApplyTreePosition(GameObject go, TraitNode asset)
        {
            int idx = ExtractTrailingNumber(asset.name) - 1;
            if (idx < 0 || idx >= LocalTreePositions.Length) return;

            float yOffset;
            switch (asset.category)
            {
                case TraitCategory.Inf:     yOffset = CategoryYInf;     break;
                case TraitCategory.Comp:    yOffset = CategoryYComp;    break;
                case TraitCategory.Stealth: yOffset = CategoryYStealth; break;
                default: yOffset = 0f; break;
            }

            var rt = go.transform as RectTransform;
            if (rt == null) return;

            Vector2 local = LocalTreePositions[idx];
            rt.anchoredPosition = new Vector2(local.x, local.y + yOffset);
        }
    }
}
#endif
