#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TraitTree.EditorTools
{
    /// <summary>
    /// 24개 샘플 TraitNode 자산을 한 번에 생성/업데이트한다.
    /// Unity 메뉴: PAYLOAD → Generate Sample Trait Tree (24 nodes)
    ///
    /// 트리 구조 (카테고리별 8노드):
    ///        N0  (root)
    ///       /  \
    ///      N1   N2
    ///     /|    |\
    ///    N3 N4 N5 N6
    ///    |
    ///    N7
    /// </summary>
    public static class TraitTreeGenerator
    {
        const string OutputFolder = "Assets/Resources/Traits";

        [MenuItem("PAYLOAD/Generate Sample Trait Tree (24 nodes)")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);

            int total = 0;
            total += GenerateBranch("Inf",     TraitCategory.Inf,     "전염");
            total += GenerateBranch("Comp",    TraitCategory.Comp,    "복잡");
            total += GenerateBranch("Stealth", TraitCategory.Stealth, "은신");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TraitTreeGenerator] {total}개 노드 생성/업데이트 완료. 위치: {OutputFolder}");
            EditorUtility.DisplayDialog(
                "Trait Tree 생성 완료",
                $"{total}개 노드를 {OutputFolder}에 생성/업데이트했습니다.\n\n" +
                "Project 창에서 자산을 확인한 뒤,\n" +
                "각 TraitNodeUI 컴포넌트의 Node 필드에 드래그해서 연결하세요.",
                "OK");
        }

        static int GenerateBranch(string idPrefix, TraitCategory category, string namePrefix)
        {
            var nodes = new TraitNode[8];

            // 카테고리별 비용/증가량 공식
            // 전염도/복잡도: cost = 50 * level, effectAmount = 8
            // 은신도:        cost = 40 * level, effectAmount = 5
            int baseCost     = (category == TraitCategory.Stealth) ? 40 : 50;
            int effectAmount = (category == TraitCategory.Stealth) ?  5 :  8;

            // 1) 자산 로드 or 생성 + 필드 갱신
            for (int i = 0; i < 8; i++)
            {
                int level = i + 1;
                string path = $"{OutputFolder}/Trait_{idPrefix}_{level}.asset";

                // 기존 자산이 있으면 그대로 로드 (씬 참조 보존을 위해 GUID 유지)
                var n = AssetDatabase.LoadAssetAtPath<TraitNode>(path);
                if (n == null)
                {
                    n = ScriptableObject.CreateInstance<TraitNode>();
                    AssetDatabase.CreateAsset(n, path);
                }

                int cost = baseCost * level;

                n.displayName = $"{namePrefix} 특성 {level}";
                n.description = $"[임시] {namePrefix} 계열 {level}단계. " +
                                $"해제 시 {CategoryKor(category)} +{effectAmount} (비용 {cost} 코인).";
                n.category     = category;
                n.cost         = cost;
                n.effectAmount = effectAmount;

                if (n.prerequisites == null) n.prerequisites = new List<TraitNode>();
                else n.prerequisites.Clear();

                nodes[i] = n;
            }

            // 2) 트리 prereq 연결
            //    N0 (root)
            //   ├─ N1
            //   │  ├─ N3 ─ N7
            //   │  └─ N4
            //   └─ N2
            //      ├─ N5
            //      └─ N6
            nodes[1].prerequisites.Add(nodes[0]);
            nodes[2].prerequisites.Add(nodes[0]);
            nodes[3].prerequisites.Add(nodes[1]);
            nodes[4].prerequisites.Add(nodes[1]);
            nodes[5].prerequisites.Add(nodes[2]);
            nodes[6].prerequisites.Add(nodes[2]);
            nodes[7].prerequisites.Add(nodes[3]);

            foreach (var n in nodes) EditorUtility.SetDirty(n);
            return nodes.Length;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static string CategoryKor(TraitCategory c)
        {
            switch (c)
            {
                case TraitCategory.Inf:     return "전염도";
                case TraitCategory.Comp:    return "복잡도";
                case TraitCategory.Stealth: return "은신도";
            }
            return "";
        }
    }
}
#endif
