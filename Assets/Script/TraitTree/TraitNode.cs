using System.Collections.Generic;
using UnityEngine;

namespace TraitTree
{
    public enum TraitCategory { Inf, Comp, Stealth }

    [CreateAssetMenu(fileName = "Trait_", menuName = "PAYLOAD/Trait Node")]
    public class TraitNode : ScriptableObject
    {
        public string displayName;
        [TextArea(2, 5)] public string description;
        public TraitCategory category;
        public List<TraitNode> prerequisites = new List<TraitNode>();

        [Header("언락 비용/효과")]
        [Tooltip("해제에 소모되는 코인")]
        public int cost = 50;

        [Tooltip("해제 시 해당 카테고리 스탯이 증가하는 양")]
        public int effectAmount = 8;
    }
}
