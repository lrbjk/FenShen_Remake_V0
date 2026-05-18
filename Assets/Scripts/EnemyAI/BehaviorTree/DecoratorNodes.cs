using System.Collections.Generic;
using UnityEngine;

namespace FenShen.EnemyAI.BehaviorTree
{
    public abstract class DecoratorNodeSO : BehaviorTreeNodeSO
    {
        public BehaviorTreeNodeSO child;

        public override IReadOnlyList<BehaviorTreeNodeSO> GetChildren()
        {
            if (child == null)
            {
                return System.Array.Empty<BehaviorTreeNodeSO>();
            }

            return new[] { child };
        }

        public override bool AddChild(BehaviorTreeNodeSO newChild)
        {
            if (newChild == null)
            {
                return false;
            }

            child = newChild;
            return true;
        }

        public override bool RemoveChild(BehaviorTreeNodeSO oldChild)
        {
            if (child != oldChild)
            {
                return false;
            }

            child = null;
            return true;
        }

        public override bool ReplaceChild(BehaviorTreeNodeSO oldChild, BehaviorTreeNodeSO newChild)
        {
            if (child != oldChild)
            {
                return false;
            }

            child = newChild;
            return true;
        }

        protected override void OnAbort(EnemyBehaviorTreeAgent agent)
        {
            if (child != null)
            {
                child.Abort(agent);
            }
        }
    }

    [CreateAssetMenu(fileName = "Inverter", menuName = "Enemy AI/Behavior Tree/Decorator/Inverter")]
    public class InverterNodeSO : DecoratorNodeSO
    {
        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (child == null)
            {
                return BehaviorTreeStatus.Failure;
            }

            BehaviorTreeStatus status = child.Tick(agent, deltaTime);
            if (status == BehaviorTreeStatus.Success)
            {
                return BehaviorTreeStatus.Failure;
            }

            if (status == BehaviorTreeStatus.Failure)
            {
                return BehaviorTreeStatus.Success;
            }

            return BehaviorTreeStatus.Running;
        }
    }

    [CreateAssetMenu(fileName = "Succeeder", menuName = "Enemy AI/Behavior Tree/Decorator/Succeeder")]
    public class SucceederNodeSO : DecoratorNodeSO
    {
        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (child == null)
            {
                return BehaviorTreeStatus.Success;
            }

            BehaviorTreeStatus status = child.Tick(agent, deltaTime);
            return status == BehaviorTreeStatus.Running ? BehaviorTreeStatus.Running : BehaviorTreeStatus.Success;
        }
    }
}
