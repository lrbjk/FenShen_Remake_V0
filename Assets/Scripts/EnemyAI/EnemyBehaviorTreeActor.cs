using System.Collections;
using FenShen.Combat;
using FenShen.EnemyAI.BehaviorTree;
using FenShen.EnemyAI.Combat;
using FenShen.GameData;
using UnityEngine;

namespace FenShen.EnemyAI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyRuntimeStatsComponent))]
    [RequireComponent(typeof(CombatHurtbox))]
    [RequireComponent(typeof(BuffController))]
    [RequireComponent(typeof(EnemyBehaviorTreeAgent))]
    [RequireComponent(typeof(EnemyCombatSkillController))]
    public class EnemyBehaviorTreeActor : MonoBehaviour
    {
        [Header("Lifecycle")]
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private bool deactivateOnDeath = true;
        [SerializeField] private float deathDelay;
        [SerializeField] private string deathAnimatorTrigger = "Death";
        [SerializeField] private bool disableAiOnDeath = true;

        [Header("Hit Feedback")]
        [SerializeField] private bool logDamage;
        [SerializeField] private string hitAnimatorTrigger = "Hit";
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.15f, 0.05f, 1f);
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

        [Header("Runtime Binding")]
        [SerializeField] private bool syncStatsToAi = true;
        [SerializeField] private Transform rootTransform;
        [SerializeField] private Renderer flashRenderer;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.1f, 0.25f);
        [SerializeField] private Vector3 gizmoSize = new Vector3(1f, 2f, 1f);

        private EnemyRuntimeStatsComponent _stats;
        private CombatHurtbox _hurtbox;
        private BuffController _buffController;
        private EnemyBehaviorTreeAgent _agent;
        private EnemyCombatSkillController _combat;
        private Animator _animator;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashRoutine;
        private bool _dead;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            CacheReferences();
            BindCombatComponents();
            SyncStatsToAi();
        }

        private void OnEnable()
        {
            CacheReferences();
            if (_stats != null)
            {
                _stats.Damaged += HandleDamaged;
                _stats.Died += HandleDied;
                _stats.StatsChanged += SyncStatsToAi;
            }
        }

        private void OnDisable()
        {
            if (_stats == null)
            {
                return;
            }

            _stats.Damaged -= HandleDamaged;
            _stats.Died -= HandleDied;
            _stats.StatsChanged -= SyncStatsToAi;
        }

        private void OnValidate()
        {
            CacheReferences();
            BindCombatComponents();
        }

        private void CacheReferences()
        {
            if (_stats == null)
            {
                _stats = GetComponent<EnemyRuntimeStatsComponent>();
            }

            if (_hurtbox == null)
            {
                _hurtbox = GetComponent<CombatHurtbox>();
            }

            if (_buffController == null)
            {
                _buffController = GetComponent<BuffController>();
            }

            if (_agent == null)
            {
                _agent = GetComponent<EnemyBehaviorTreeAgent>();
            }

            if (_combat == null)
            {
                _combat = GetComponent<EnemyCombatSkillController>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (rootTransform == null)
            {
                rootTransform = transform;
            }

            if (flashRenderer == null)
            {
                flashRenderer = GetComponentInChildren<Renderer>();
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void BindCombatComponents()
        {
            if (_hurtbox != null)
            {
                _hurtbox.Configure(CombatTeam.Enemy, _stats, rootTransform != null ? rootTransform : transform);
            }

            if (_combat != null)
            {
                if (_combat.character == null)
                {
                    _combat.character = rootTransform != null ? rootTransform : transform;
                }

                if (_combat.animator == null)
                {
                    _combat.animator = _animator;
                }

                if (_combat.runtimeStats == null)
                {
                    _combat.runtimeStats = _stats;
                }

                _combat.team = CombatTeam.Enemy;
            }
        }

        private void SyncStatsToAi()
        {
            if (!syncStatsToAi || _stats == null)
            {
                return;
            }

            if (_agent != null)
            {
                float moveSpeed = _stats.GetStat(StatKeys.MoveSpeed);
                float detectRange = _stats.GetStat(StatKeys.DetectRange);
                float attackRange = _stats.GetStat(StatKeys.AttackRange);
                float attackCooldown = _stats.GetStat(StatKeys.AttackCooldown);

                if (moveSpeed > 0f)
                {
                    _agent.moveSpeed = moveSpeed;
                }

                if (detectRange > 0f)
                {
                    _agent.detectionRange = detectRange;
                }

                if (attackRange > 0f)
                {
                    _agent.attackRange = attackRange;
                }

                if (attackCooldown > 0f)
                {
                    _agent.attackCooldown = attackCooldown;
                }
            }

            if (_combat != null)
            {
                float attack = _stats.GetStat(StatKeys.Attack);
                if (attack > 0f)
                {
                    _combat.fallbackBaseAttack = attack;
                }
            }
        }

        private void HandleDamaged(float damage)
        {
            if (_dead)
            {
                return;
            }

            if (logDamage && _stats != null)
            {
                Debug.Log($"[EnemyAI] {name} took {damage:0.##} damage. HP: {_stats.CurrentHP:0.##}/{_stats.MaxHP:0.##}", this);
            }

            if (_animator != null && !string.IsNullOrWhiteSpace(hitAnimatorTrigger))
            {
                _animator.SetTrigger(hitAnimatorTrigger);
            }

            FlashHit();
        }

        private void HandleDied()
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            if (logDamage)
            {
                Debug.Log($"[EnemyAI] {name} died.", this);
            }

            if (_animator != null && !string.IsNullOrWhiteSpace(deathAnimatorTrigger))
            {
                _animator.SetTrigger(deathAnimatorTrigger);
            }

            if (disableAiOnDeath)
            {
                if (_agent != null)
                {
                    _agent.enabled = false;
                }

                if (_combat != null)
                {
                    _combat.ForceStopSkill();
                    _combat.enabled = false;
                }
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject, Mathf.Max(0f, deathDelay));
                return;
            }

            if (deactivateOnDeath)
            {
                StartCoroutine(DeactivateAfterDelay());
            }
        }

        private IEnumerator DeactivateAfterDelay()
        {
            if (deathDelay > 0f)
            {
                yield return new WaitForSeconds(deathDelay);
            }

            gameObject.SetActive(false);
        }

        private void FlashHit()
        {
            if (flashRenderer == null || hitFlashDuration <= 0f)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashHitRoutine());
        }

        private IEnumerator FlashHitRoutine()
        {
            flashRenderer.GetPropertyBlock(_propertyBlock);
            Color originalColor = Color.white;
            bool useBaseColor = flashRenderer.sharedMaterial != null && flashRenderer.sharedMaterial.HasProperty(BaseColorId);
            bool useColor = flashRenderer.sharedMaterial != null && flashRenderer.sharedMaterial.HasProperty(ColorId);
            if (useBaseColor)
            {
                originalColor = flashRenderer.sharedMaterial.GetColor(BaseColorId);
            }
            else if (useColor)
            {
                originalColor = flashRenderer.sharedMaterial.GetColor(ColorId);
            }

            _propertyBlock.SetColor(useBaseColor ? BaseColorId : ColorId, hitFlashColor);
            flashRenderer.SetPropertyBlock(_propertyBlock);

            yield return new WaitForSeconds(hitFlashDuration);

            _propertyBlock.SetColor(useBaseColor ? BaseColorId : ColorId, originalColor);
            flashRenderer.SetPropertyBlock(_propertyBlock);
            _flashRoutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Transform root = rootTransform != null ? rootTransform : transform;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(root.position, gizmoSize);
        }
    }
}
