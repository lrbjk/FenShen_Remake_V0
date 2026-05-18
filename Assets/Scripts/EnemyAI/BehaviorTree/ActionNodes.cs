using UnityEngine;
using FenShen.Combat;

namespace FenShen.EnemyAI.BehaviorTree
{
    [CreateAssetMenu(fileName = "Wait", menuName = "Enemy AI/Behavior Tree/Action/Wait")]
    public class WaitNodeSO : BehaviorTreeNodeSO
    {
        public float duration = 1f;
        private float _elapsed;

        protected override void OnStart(EnemyBehaviorTreeAgent agent)
        {
            _elapsed = 0f;
        }

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            _elapsed += deltaTime;
            return _elapsed >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
        }
    }

    [CreateAssetMenu(fileName = "MoveToTarget", menuName = "Enemy AI/Behavior Tree/Action/Move To Target")]
    public class MoveToTargetNodeSO : BehaviorTreeNodeSO
    {
        public float stoppingDistance = 1.25f;

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (agent == null || !agent.HasTarget)
            {
                return BehaviorTreeStatus.Failure;
            }

            if (agent.DistanceToTarget <= stoppingDistance)
            {
                agent.StopMoving();
                return BehaviorTreeStatus.Success;
            }

            agent.MoveTowardsTarget(deltaTime, stoppingDistance);
            return BehaviorTreeStatus.Running;
        }

        protected override void OnStop(EnemyBehaviorTreeAgent agent, BehaviorTreeStatus status)
        {
            if (agent != null)
            {
                agent.StopMoving();
            }
        }
    }

    [CreateAssetMenu(fileName = "AttackTarget", menuName = "Enemy AI/Behavior Tree/Action/Attack Target")]
    public class AttackTargetNodeSO : BehaviorTreeNodeSO
    {
        public CombatSkillDefinitionSO skill;
        public bool failWhenCooldown;

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (agent == null || !agent.HasTarget)
            {
                return BehaviorTreeStatus.Failure;
            }

            if (!agent.CanStartCombatSkill(skill))
            {
                return failWhenCooldown ? BehaviorTreeStatus.Failure : BehaviorTreeStatus.Running;
            }

            return agent.TryStartCombatSkill(skill) ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }
    }

    [CreateAssetMenu(fileName = "PlayAnimation", menuName = "Enemy AI/Behavior Tree/Action/Play Animation")]
    public class PlayAnimationNodeSO : BehaviorTreeNodeSO
    {
        public string stateName;
        public int layer;
        public bool crossFade = true;
        public float transitionDuration = 0.05f;

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (agent == null || string.IsNullOrWhiteSpace(stateName))
            {
                return BehaviorTreeStatus.Failure;
            }

            agent.PlayAnimationState(stateName, layer, crossFade, transitionDuration);
            return BehaviorTreeStatus.Success;
        }
    }

    [CreateAssetMenu(fileName = "Patrol", menuName = "Enemy AI/Behavior Tree/Action/Patrol")]
    public class PatrolNodeSO : BehaviorTreeNodeSO
    {
        public float arriveDistance = 0.15f;
        public bool loop = true;
        private int _waypointIndex;

        protected override void OnStart(EnemyBehaviorTreeAgent agent)
        {
            _waypointIndex = agent != null ? agent.PatrolIndex : 0;
        }

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (agent == null || agent.PatrolPointCount == 0)
            {
                return BehaviorTreeStatus.Failure;
            }

            if (_waypointIndex >= agent.PatrolPointCount)
            {
                return BehaviorTreeStatus.Success;
            }

            bool arrived = agent.MoveTowardsPatrolPoint(_waypointIndex, deltaTime, arriveDistance);
            if (!arrived)
            {
                return BehaviorTreeStatus.Running;
            }

            _waypointIndex++;
            if (_waypointIndex >= agent.PatrolPointCount)
            {
                if (!loop)
                {
                    agent.PatrolIndex = _waypointIndex;
                    return BehaviorTreeStatus.Success;
                }

                _waypointIndex = 0;
            }

            agent.PatrolIndex = _waypointIndex;
            return BehaviorTreeStatus.Running;
        }

        protected override void OnStop(EnemyBehaviorTreeAgent agent, BehaviorTreeStatus status)
        {
            if (agent != null)
            {
                agent.StopMoving();
            }
        }
    }

    [CreateAssetMenu(fileName = "SetAnimatorTrigger", menuName = "Enemy AI/Behavior Tree/Action/Set Animator Trigger")]
    public class SetAnimatorTriggerNodeSO : BehaviorTreeNodeSO
    {
        public string triggerName;

        protected override BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (agent == null || string.IsNullOrWhiteSpace(triggerName))
            {
                return BehaviorTreeStatus.Failure;
            }

            agent.SetAnimatorTrigger(triggerName);
            return BehaviorTreeStatus.Success;
        }
    }
}
