using FenShen.GameData;
using System.Collections;
using UnityEngine;

namespace FenShen.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TestEnemyRuntimeStatsComponent))]
    [RequireComponent(typeof(CombatHurtbox))]
    [RequireComponent(typeof(BuffController))]
    public class CombatTestEnemy : MonoBehaviour
    {
        [Header("测试敌人")]
        [InspectorName("死亡时销毁")]
        [SerializeField] private bool destroyOnDeath;
        [InspectorName("死亡时禁用物体")]
        [SerializeField] private bool deactivateOnDeath = true;
        [InspectorName("打印受击日志")]
        [SerializeField] private bool logDamage = true;
        [InspectorName("绘制测试体积")]
        [SerializeField] private bool drawGizmos = true;
        [InspectorName("测试体积颜色")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.1f, 0.25f);
        [InspectorName("测试体积大小")]
        [SerializeField] private Vector3 gizmoSize = new Vector3(1f, 2f, 1f);
        [InspectorName("受击闪烁颜色")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.15f, 0.05f, 1f);
        [InspectorName("受击闪烁时间")]
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

        private TestEnemyRuntimeStatsComponent _stats;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashRoutine;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            _stats = GetComponent<TestEnemyRuntimeStatsComponent>();
            _renderer = GetComponentInChildren<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            if (_stats == null)
            {
                _stats = GetComponent<TestEnemyRuntimeStatsComponent>();
            }

            _stats.Damaged += HandleDamaged;
            _stats.Died += HandleDied;
        }

        void OnDisable()
        {
            if (_stats == null)
            {
                return;
            }

            _stats.Damaged -= HandleDamaged;
            _stats.Died -= HandleDied;
        }

        private void HandleDamaged(float damage)
        {
            if (!logDamage)
            {
                FlashHit();
                return;
            }

            Debug.Log($"[CombatTestEnemy] {name} took {damage:0.##} damage. HP: {_stats.CurrentHP:0.##}/{_stats.MaxHP:0.##}", this);
            FlashHit();
        }

        private void HandleDied()
        {
            if (logDamage)
            {
                Debug.Log($"[CombatTestEnemy] {name} died.", this);
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
                return;
            }

            if (deactivateOnDeath)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(transform.position, gizmoSize);
        }

        private void FlashHit()
        {
            if (_renderer == null || hitFlashDuration <= 0f)
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
            _renderer.GetPropertyBlock(_propertyBlock);
            Color originalColor = Color.white;
            bool useBaseColor = _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(BaseColorId);
            bool useColor = _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(ColorId);
            if (useBaseColor)
            {
                originalColor = _renderer.sharedMaterial.GetColor(BaseColorId);
            }
            else if (useColor)
            {
                originalColor = _renderer.sharedMaterial.GetColor(ColorId);
            }

            _propertyBlock.SetColor(useBaseColor ? BaseColorId : ColorId, hitFlashColor);
            _renderer.SetPropertyBlock(_propertyBlock);

            yield return new WaitForSeconds(hitFlashDuration);

            _propertyBlock.SetColor(useBaseColor ? BaseColorId : ColorId, originalColor);
            _renderer.SetPropertyBlock(_propertyBlock);
            _flashRoutine = null;
        }
    }
}
