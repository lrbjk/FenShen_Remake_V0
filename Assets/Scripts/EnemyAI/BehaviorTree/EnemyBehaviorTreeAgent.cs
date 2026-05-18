using UnityEngine;
using FenShen.Combat;
using FenShen.EnemyAI;
using FenShen.EnemyAI.Combat;
using FenShen.GameData;

namespace FenShen.EnemyAI.BehaviorTree
{
    [RequireComponent(typeof(EnemyRuntimeStatsComponent))]
    [RequireComponent(typeof(CombatHurtbox))]
    [RequireComponent(typeof(BuffController))]
    [RequireComponent(typeof(EnemyCombatSkillController))]
    public class EnemyBehaviorTreeAgent : MonoBehaviour
    {
        [Header("Behavior Tree")]
        public BehaviorTreeGraphSO behaviorTree;
        [Min(0f)] public float tickInterval = 0f;

        [Header("References")]
        public Transform body;
        public Animator animator;
        public Rigidbody2D body2D;
        public EnemyCombatSkillController combatController;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public bool faceMoveDirection = true;
        public Transform[] patrolPoints;

        [Header("Combat")]
        public float attackRange = 1.4f;
        public float attackCooldown = 1.2f;
        public string attackTrigger = "Attack";
        public CombatSkillDefinitionSO defaultAttackSkill;

        [Header("Sensing")]
        public LayerMask targetLayers;
        public float detectionRange = 8f;
        [Min(0.02f)] public float targetScanInterval = 0.15f;
        public LayerMask lineOfSightMask = ~0;

        [Header("Debug")]
        public bool showDebugOverlay;
        public Vector2 debugOverlayPosition = new Vector2(16f, 180f);
        public Vector2 debugOverlaySize = new Vector2(320f, 110f);

        private BehaviorTreeNodeSO _runtimeRoot;
        private float _tickTimer;
        private float _targetScanTimer;
        private float _lastAttackTime = float.NegativeInfinity;
        private BehaviorTreeStatus _lastStatus = BehaviorTreeStatus.Failure;
        private Transform _target;

        public int PatrolIndex { get; set; }
        public BehaviorTreeStatus LastStatus { get { return _lastStatus; } }
        public Transform Target { get { return _target; } }
        public bool HasTarget { get { return _target != null && DistanceToTarget <= detectionRange; } }
        public float DistanceToTarget { get { return _target != null ? Vector2.Distance(transform.position, _target.position) : float.PositiveInfinity; } }
        public bool IsAttackReady { get { return Time.time - _lastAttackTime >= attackCooldown; } }
        public int PatrolPointCount { get { return patrolPoints != null ? patrolPoints.Length : 0; } }

        private void Reset()
        {
            body = transform;
            animator = GetComponentInChildren<Animator>();
            body2D = GetComponent<Rigidbody2D>();
            combatController = GetComponent<EnemyCombatSkillController>();
            int playerLayerMask = LayerMask.GetMask("Player");
            if (playerLayerMask != 0)
            {
                targetLayers = playerLayerMask;
            }
        }

        private void OnEnable()
        {
            RebuildRuntimeTree();
        }

        private void OnDisable()
        {
            if (_runtimeRoot != null)
            {
                _runtimeRoot.Abort(this);
            }
        }

        private void Update()
        {
            RefreshTarget(Time.deltaTime);
            SyncCombatTarget();

            if (_runtimeRoot == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (tickInterval > 0f)
            {
                _tickTimer -= dt;
                if (_tickTimer > 0f)
                {
                    return;
                }

                _tickTimer = tickInterval;
            }

            _lastStatus = _runtimeRoot.Tick(this, dt);
        }

        public void RebuildRuntimeTree()
        {
            if (_runtimeRoot != null)
            {
                _runtimeRoot.Abort(this);
            }

            _runtimeRoot = behaviorTree != null ? behaviorTree.CreateRuntimeRoot() : null;
            _tickTimer = 0f;
        }

        public void MoveTowardsTarget(float deltaTime, float stoppingDistance)
        {
            if (_target == null)
            {
                StopMoving();
                return;
            }

            MoveTowards(_target.position, deltaTime, stoppingDistance);
        }

        public bool MoveTowardsPatrolPoint(int index, float deltaTime, float arriveDistance)
        {
            if (patrolPoints == null || index < 0 || index >= patrolPoints.Length || patrolPoints[index] == null)
            {
                StopMoving();
                return true;
            }

            return MoveTowards(patrolPoints[index].position, deltaTime, arriveDistance);
        }

        public bool MoveTowards(Vector3 worldPosition, float deltaTime, float stoppingDistance)
        {
            Vector2 current = body2D != null ? body2D.position : (Vector2)transform.position;
            Vector2 destination = worldPosition;
            Vector2 offset = destination - current;
            float distance = offset.magnitude;
            if (distance <= stoppingDistance)
            {
                StopMoving();
                return true;
            }

            Vector2 direction = offset / Mathf.Max(distance, 0.0001f);
            Vector2 next = current + direction * moveSpeed * deltaTime;
            if (body2D != null)
            {
                body2D.MovePosition(next);
            }
            else
            {
                transform.position = next;
            }

            if (faceMoveDirection && Mathf.Abs(direction.x) > 0.01f)
            {
                Transform visual = body != null ? body : transform;
                Vector3 scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
                visual.localScale = scale;
            }

            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", moveSpeed);
                animator.SetBool("Moving", true);
            }

            return false;
        }

        public void StopMoving()
        {
            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", 0f);
                animator.SetBool("Moving", false);
            }
        }

        public void AttackTarget()
        {
            TryStartCombatSkill(defaultAttackSkill);
        }

        public bool TryStartCombatSkill(CombatSkillDefinitionSO skill)
        {
            if (combatController != null)
            {
                combatController.SetTarget(_target);
                bool started = combatController.TryStartSkill(skill);
                if (started)
                {
                    _lastAttackTime = Time.time;
                    StopMoving();
                }

                return started;
            }

            _lastAttackTime = Time.time;
            StopMoving();
            SetAnimatorTrigger(attackTrigger);
            SendMessage("OnBehaviorTreeAttack", _target, SendMessageOptions.DontRequireReceiver);
            return true;
        }

        public bool CanStartCombatSkill(CombatSkillDefinitionSO skill)
        {
            if (combatController != null)
            {
                return combatController.CanStartSkill(skill);
            }

            return IsAttackReady;
        }

        public void PlayAnimationState(string stateName, int layer, bool crossFade, float transitionDuration)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (crossFade)
            {
                animator.CrossFadeInFixedTime(stateName, transitionDuration, layer);
                return;
            }

            animator.Play(stateName, layer, 0f);
        }

        public void SetAnimatorTrigger(string triggerName)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        public bool HasLineOfSightToTarget()
        {
            if (_target == null)
            {
                return false;
            }

            Vector2 origin = transform.position;
            Vector2 destination = _target.position;
            RaycastHit2D hit = Physics2D.Linecast(origin, destination, lineOfSightMask);
            return hit.collider == null || hit.transform == _target || hit.transform.IsChildOf(_target);
        }

        private void RefreshTarget(float deltaTime)
        {
            _targetScanTimer -= deltaTime;
            if (_targetScanTimer > 0f && IsCurrentTargetValid())
            {
                return;
            }

            _targetScanTimer = targetScanInterval;
            _target = FindNearestTarget();
        }

        private bool IsCurrentTargetValid()
        {
            if (_target == null)
            {
                return false;
            }

            if (DistanceToTarget > detectionRange)
            {
                return false;
            }

            return true;
        }

        private Transform FindNearestTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, targetLayers);
            Transform best = null;
            float bestSqrDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                Transform candidate = ResolveTargetRoot(hit);
                if (candidate == null || candidate == transform || candidate.IsChildOf(transform))
                {
                    continue;
                }

                float sqrDistance = ((Vector2)candidate.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = candidate;
                }
            }

            return best;
        }

        private Transform ResolveTargetRoot(Collider2D hit)
        {
            CombatHurtbox hurtbox = hit.GetComponentInParent<CombatHurtbox>();
            if (hurtbox != null)
            {
                return hurtbox.RootTransform;
            }

            return hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
        }

        private void SyncCombatTarget()
        {
            if (combatController != null)
            {
                combatController.SetTarget(_target);
            }
        }

        private void OnGUI()
        {
            if (!showDebugOverlay)
            {
                return;
            }

            Rect rect = new Rect(debugOverlayPosition.x, debugOverlayPosition.y, debugOverlaySize.x, debugOverlaySize.y);
            GUI.Box(rect, "Enemy Behavior Tree");
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, rect.height - 40f));
            GUILayout.Label("Tree: " + (behaviorTree != null ? behaviorTree.name : "None"));
            GUILayout.Label("Status: " + _lastStatus);
            GUILayout.Label("Target: " + (_target != null ? _target.name : "None"));
            GUILayout.EndArea();
        }
    }
}
