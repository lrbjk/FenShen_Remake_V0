using System.Collections.Generic;
using UnityEngine;

namespace FenShen.EnemyAI.BehaviorTree
{
    public abstract class CompositeNodeSO : BehaviorTreeNodeSO
    {
        public List<BehaviorTreeNodeSO> children = new List<BehaviorTreeNodeSO>();

        public override IReadOnlyList<BehaviorTreeNodeSO> GetChildren()
        {
            return children;
        }

        public override bool AddChild(BehaviorTreeNodeSO child)
        {
            if (child == null || children.Contains(child))
            {
                return false;
            }

            children.Add(child);
            return true;
        }

        public override bool RemoveChild(BehaviorTreeNodeSO child)
        {
            return child != null && children.Remove(child);
        }

        public override bool ReplaceChild(BehaviorTreeNodeSO oldChild, BehaviorTreeNodeSO newChild)
        {
            int index = children.IndexOf(oldChild);
            if (index < 0)
            {
                return false;
            }

            children[index] = newChild;
            return true;
        }

        public void SortChildrenByPosition()
        {
            children.Sort((a, b) =>
            {
                float ax = a != null ? a.editorPosition.x : 0f;
                float bx = b != null ? b.editorPosition.x : 0f;
                return ax.CompareTo(bx);
            });
        }
    }

    [CreateAssetMenu(fileName = "Sequence", menuName = "Enemy AI/Behavior Tree/Composite/Sequence")]
    public class SequenceNodeSO : CompositeNodeSO
    {
        private int _currentIndex;

        protected override void OnStart(EnemyBehaviorTreeAgent agent)
        {
            _currentIndex = 0;
        }

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (children.Count == 0)
            {
                return BehaviorTreeStatus.Success;
            }

            while (_currentIndex < children.Count)
            {
                BehaviorTreeNodeSO child = children[_currentIndex];
                if (child == null)
                {
                    _currentIndex++;
                    continue;
                }

                BehaviorTreeStatus status = child.Tick(agent, deltaTime);
                if (status == BehaviorTreeStatus.Running || status == BehaviorTreeStatus.Failure)
                {
                    return status;
                }

                _currentIndex++;
            }

            return BehaviorTreeStatus.Success;
        }

        protected override void OnAbort(EnemyBehaviorTreeAgent agent)
        {
            AbortRunningChild(agent);
        }

        private void AbortRunningChild(EnemyBehaviorTreeAgent agent)
        {
            if (_currentIndex >= 0 && _currentIndex < children.Count && children[_currentIndex] != null)
            {
                children[_currentIndex].Abort(agent);
            }
        }
    }

    [CreateAssetMenu(fileName = "Selector", menuName = "Enemy AI/Behavior Tree/Composite/Selector")]
    public class SelectorNodeSO : CompositeNodeSO
    {
        private int _currentIndex;

        protected override void OnStart(EnemyBehaviorTreeAgent agent)
        {
            _currentIndex = 0;
        }

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (children.Count == 0)
            {
                return BehaviorTreeStatus.Failure;
            }

            while (_currentIndex < children.Count)
            {
                BehaviorTreeNodeSO child = children[_currentIndex];
                if (child == null)
                {
                    _currentIndex++;
                    continue;
                }

                BehaviorTreeStatus status = child.Tick(agent, deltaTime);
                if (status == BehaviorTreeStatus.Running || status == BehaviorTreeStatus.Success)
                {
                    return status;
                }

                _currentIndex++;
            }

            return BehaviorTreeStatus.Failure;
        }

        protected override void OnAbort(EnemyBehaviorTreeAgent agent)
        {
            if (_currentIndex >= 0 && _currentIndex < children.Count && children[_currentIndex] != null)
            {
                children[_currentIndex].Abort(agent);
            }
        }
    }
}
