using FenShen.Combat;
using FenShen.GameData;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    public class PlayerCloneRuntime : MonoBehaviour
    {
        private enum CloneMode
        {
            None,
            Decoy,
            Assault,
            Mimic,
        }

        private CloneMode _mode;
        private Transform _target;
        private float _moveSpeed;
        private float _damage;
        private float _hitRadius;
        private float _ownerAttack;
        private float _mimicDamageMultiplier;
        private float _expireTime;
        private bool _assaultHitApplied;
        private LayerMask _targetLayers;
        private CombatCoordinator _ownerCombat;
        private PooledObject _pooledObject;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        [Header("运行时兜底")]
        [InspectorName("缺少物理组件时自动补齐")]
        [SerializeField] private bool autoAddMissingPhysicsComponents = true;
        [InspectorName("运行时修改分身外观")]
        [SerializeField] private bool overrideRuntimeVisual;

        private Color _gizmoColor = Color.white;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public bool IsAvailableForSwap
        {
            get
            {
                return (_mode == CloneMode.Assault || _mode == CloneMode.Decoy)
                    && gameObject.activeInHierarchy
                    && Time.time < _expireTime;
            }
        }

        void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
            _renderer = GetComponentInChildren<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            EnsureRuntimeComponents();
        }

        void OnEnable()
        {
            _assaultHitApplied = false;
        }

        void OnDisable()
        {
            UnsubscribeMimic();
        }

        void Update()
        {
            if (_mode == CloneMode.None)
            {
                return;
            }

            if (Time.time >= _expireTime)
            {
                Release();
                return;
            }

            if (_mode == CloneMode.Assault)
            {
                TickAssault();
            }
        }

        public void InitializeDecoy(float lifetime)
        {
            ResetRuntime();
            _mode = CloneMode.Decoy;
            _expireTime = Time.time + Mathf.Max(0.01f, lifetime);
            EnsureRuntimeComponents();
            ConfigureVisual(new Color(0.25f, 0.75f, 1f, 0.85f));
            PlayerCloneDecoyTarget decoyTarget = GetComponent<PlayerCloneDecoyTarget>();
            if (decoyTarget == null)
            {
                decoyTarget = gameObject.AddComponent<PlayerCloneDecoyTarget>();
            }

            decoyTarget.Initialize(_expireTime);
        }

        public void InitializeAssault(Transform target, float moveSpeed, float damage, float hitRadius, LayerMask targetLayers, float lifetime)
        {
            ResetRuntime();
            _mode = CloneMode.Assault;
            _target = target;
            _moveSpeed = moveSpeed;
            _damage = damage;
            _hitRadius = hitRadius;
            _targetLayers = targetLayers;
            _expireTime = Time.time + Mathf.Max(0.01f, lifetime);
            EnsureRuntimeComponents();
            ConfigureVisual(new Color(1f, 0.35f, 0.15f, 0.95f));
        }

        public void InitializeMimic(CombatCoordinator ownerCombat, float ownerAttack, float damageMultiplier, float hitRadius, LayerMask targetLayers, float lifetime)
        {
            ResetRuntime();
            _mode = CloneMode.Mimic;
            _ownerCombat = ownerCombat;
            _ownerAttack = Mathf.Max(1f, ownerAttack);
            _mimicDamageMultiplier = damageMultiplier;
            _hitRadius = hitRadius;
            _targetLayers = targetLayers;
            _expireTime = Time.time + Mathf.Max(0.01f, lifetime);
            EnsureRuntimeComponents();
            ConfigureVisual(new Color(0.35f, 1f, 0.85f, 0.9f));
            if (_ownerCombat != null)
            {
                _ownerCombat.SkillStarted += HandleOwnerSkillStarted;
            }
        }

        public void Release()
        {
            UnsubscribeMimic();
            _mode = CloneMode.None;
            if (_pooledObject != null && _pooledObject.IsSpawned)
            {
                _pooledObject.Release();
                return;
            }

            gameObject.SetActive(false);
        }

        private void TickAssault()
        {
            if (_target == null)
            {
                Release();
                return;
            }

            Vector3 toTarget = _target.position - transform.position;
            if (toTarget.sqrMagnitude > _hitRadius * _hitRadius)
            {
                Vector3 delta = toTarget.normalized * (_moveSpeed * Time.deltaTime);
                transform.position += delta;
                FaceDirection(toTarget);
                return;
            }

            if (!_assaultHitApplied)
            {
                ApplyAreaDamage(_damage);
                _assaultHitApplied = true;
            }
        }

        private void HandleOwnerSkillStarted(CombatSkillDefinitionSO skill)
        {
            if (_mode != CloneMode.Mimic || skill == null)
            {
                return;
            }

            float baseDamage = _ownerAttack * Mathf.Max(0f, skill.damageMultiplier);
            ApplyAreaDamage(baseDamage * _mimicDamageMultiplier);
        }

        private void ApplyAreaDamage(float damage)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Mathf.Max(0.01f, _hitRadius), _targetLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                CombatHurtbox hurtbox = hit.GetComponentInParent<CombatHurtbox>();
                if (hurtbox == null || hurtbox.Team == CombatTeam.Player)
                {
                    continue;
                }

                hurtbox.ReceiveDamage(new DamageInfo(
                    damage,
                    DamageType.Physical,
                    gameObject,
                    gameObject,
                    hit.ClosestPoint(transform.position),
                    hurtbox.RootTransform.position - transform.position));
            }

            Collider2D[] hits2D = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.01f, _hitRadius), _targetLayers);
            for (int i = 0; i < hits2D.Length; i++)
            {
                Collider2D hit = hits2D[i];
                if (hit == null)
                {
                    continue;
                }

                CombatHurtbox hurtbox = hit.GetComponentInParent<CombatHurtbox>();
                if (hurtbox == null || hurtbox.Team == CombatTeam.Player)
                {
                    continue;
                }

                Vector3 hitPoint = hit.ClosestPoint(transform.position);
                hurtbox.ReceiveDamage(new DamageInfo(
                    damage,
                    DamageType.Physical,
                    gameObject,
                    gameObject,
                    hitPoint,
                    hurtbox.RootTransform.position - transform.position));
            }
        }

        private void ResetRuntime()
        {
            UnsubscribeMimic();
            _mode = CloneMode.None;
            _target = null;
            _moveSpeed = 0f;
            _damage = 0f;
            _hitRadius = 0f;
            _ownerAttack = 1f;
            _mimicDamageMultiplier = 1f;
            _expireTime = float.PositiveInfinity;
            _assaultHitApplied = false;
            _ownerCombat = null;
            PlayerCloneDecoyTarget decoyTarget = GetComponent<PlayerCloneDecoyTarget>();
            if (decoyTarget != null)
            {
                decoyTarget.Clear();
            }
        }

        private void UnsubscribeMimic()
        {
            if (_ownerCombat != null)
            {
                _ownerCombat.SkillStarted -= HandleOwnerSkillStarted;
                _ownerCombat = null;
            }
        }

        private void EnsureRuntimeComponents()
        {
            if (!autoAddMissingPhysicsComponents)
            {
                return;
            }

            Collider collider3D = GetComponent<Collider>();
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider3D == null && collider2D == null)
            {
                CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.45f;
            }

            if (collider3D != null || collider2D == null)
            {
                Rigidbody rigidbody = GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = gameObject.AddComponent<Rigidbody>();
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                }
            }
            else
            {
                Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
                if (rigidbody2D == null)
                {
                    rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
                    rigidbody2D.gravityScale = 0f;
                    rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
                }
            }
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void ConfigureVisual(Color color)
        {
            _gizmoColor = color;
            if (!overrideRuntimeVisual)
            {
                return;
            }

            if (_renderer == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            int propertyId = ResolveColorPropertyId();
            _propertyBlock.SetColor(propertyId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private int ResolveColorPropertyId()
        {
            if (_renderer != null && _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(BaseColorId))
            {
                return BaseColorId;
            }

            return ColorId;
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (!overrideRuntimeVisual)
            {
                return;
            }

            float x = direction.x < 0f ? -Mathf.Abs(transform.localScale.x) : Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
        }

        private void OnDrawGizmosSelected()
        {
            if (_mode == CloneMode.None)
            {
                return;
            }

            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, _hitRadius));
        }
    }
}
