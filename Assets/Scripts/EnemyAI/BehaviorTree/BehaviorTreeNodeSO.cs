using System.Collections.Generic;
using UnityEngine;

namespace FenShen.EnemyAI.BehaviorTree
{
    public abstract class BehaviorTreeNodeSO : ScriptableObject
    {
        [Header("Editor")]
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Vector2 editorPosition = new Vector2(120f, 120f);

        [System.NonSerialized] private bool _started;

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(displayName) ? name : displayName; }
        }

        public BehaviorTreeStatus Tick(EnemyBehaviorTreeAgent agent, float deltaTime)
        {
            if (!_started)
            {
                _started = true;
                OnStart(agent);
            }

            BehaviorTreeStatus status = OnUpdate(agent, deltaTime);
            if (status != BehaviorTreeStatus.Running)
            {
                OnStop(agent, status);
                _started = false;
            }

            return status;
        }

        public void Abort(EnemyBehaviorTreeAgent agent)
        {
            if (!_started)
            {
                return;
            }

            OnAbort(agent);
            _started = false;
        }

        public virtual IReadOnlyList<BehaviorTreeNodeSO> GetChildren()
        {
            return System.Array.Empty<BehaviorTreeNodeSO>();
        }

        public virtual bool AddChild(BehaviorTreeNodeSO child)
        {
            return false;
        }

        public virtual bool RemoveChild(BehaviorTreeNodeSO child)
        {
            return false;
        }

        public virtual bool ReplaceChild(BehaviorTreeNodeSO oldChild, BehaviorTreeNodeSO newChild)
        {
            return false;
        }

        protected virtual void OnStart(EnemyBehaviorTreeAgent agent) { }
        protected abstract BehaviorTreeStatus OnUpdate(EnemyBehaviorTreeAgent agent, float deltaTime);
        protected virtual void OnStop(EnemyBehaviorTreeAgent agent, BehaviorTreeStatus status) { }
        protected virtual void OnAbort(EnemyBehaviorTreeAgent agent) { }
    }
}
