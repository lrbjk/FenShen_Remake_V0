using UnityEngine;

namespace FenShen.EnemyAI.BehaviorTree
{
    [CreateAssetMenu(fileName = "HasTarget", menuName = "Enemy AI/Behavior Tree/Condition/Has Target")]
    public class HasTargetNodeSO : BehaviorTreeNodeSO
    {
        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            return agent != null && agent.HasTarget ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }
    }

    [CreateAssetMenu(fileName = "TargetInRange", menuName = "Enemy AI/Behavior Tree/Condition/Target In Range")]
    public class TargetInRangeNodeSO : BehaviorTreeNodeSO
    {
        public float range = 2f;
        public bool requireLineOfSight;

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (agent == null || !agent.HasTarget)
            {
                return BehaviorTreeStatus.Failure;
            }

            bool inRange = agent.DistanceToTarget <= range;
            if (!inRange)
            {
                return BehaviorTreeStatus.Failure;
            }

            if (requireLineOfSight && !agent.HasLineOfSightToTarget())
            {
                return BehaviorTreeStatus.Failure;
            }

            return BehaviorTreeStatus.Success;
        }
    }

    [CreateAssetMenu(fileName = "AttackReady", menuName = "Enemy AI/Behavior Tree/Condition/Attack Ready")]
    public class AttackReadyNodeSO : BehaviorTreeNodeSO
    {
        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            return agent != null && agent.IsAttackReady ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }
    }
}
