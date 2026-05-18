using System.Collections.Generic;
using UnityEngine;

namespace FenShen.EnemyAI.BehaviorTree
{
    [CreateAssetMenu(fileName = "EnemyBehaviorTree", menuName = "Enemy AI/Behavior Tree")]
    public class BehaviorTreeGraphSO : ScriptableObject
    {
        public BehaviorTreeNodeSO rootNode;
        public List<BehaviorTreeNodeSO> nodes = new List<BehaviorTreeNodeSO>();

        public BehaviorTreeNodeSO CreateRuntimeRoot()
        {
            var cloned = new Dictionary<BehaviorTreeNodeSO, BehaviorTreeNodeSO>();
            return CloneNode(rootNode, cloned);
        }

        public bool ContainsNode(BehaviorTreeNodeSO node)
        {
            return node != null && nodes.Contains(node);
        }

        private BehaviorTreeNodeSO CloneNode(BehaviorTreeNodeSO source, Dictionary<BehaviorTreeNodeSO, BehaviorTreeNodeSO> cloned)
        {
            if (source == null)
            {
                return null;
            }

            BehaviorTreeNodeSO existing;
            if (cloned.TryGetValue(source, out existing))
            {
                return existing;
            }

            BehaviorTreeNodeSO copy = Instantiate(source);
            copy.name = source.name;
            cloned[source] = copy;

            IReadOnlyList<BehaviorTreeNodeSO> sourceChildren = source.GetChildren();
            for (int i = 0; i < sourceChildren.Count; i++)
            {
                BehaviorTreeNodeSO childCopy = CloneNode(sourceChildren[i], cloned);
                if (childCopy != null)
                {
                    copy.ReplaceChild(sourceChildren[i], childCopy);
                }
            }

            return copy;
        }
    }
}
