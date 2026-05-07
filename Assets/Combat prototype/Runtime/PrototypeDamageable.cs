using System.Collections;
using UnityEngine;

namespace FenShen.CombatPrototype
{
    public class PrototypeDamageable : MonoBehaviour
    {
        public PrototypeTeam team = PrototypeTeam.Enemy;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public bool destroyOnDeath = true;
        public float hitFlashTime = 0.08f;

        private SpriteRenderer _renderer;
        private Rigidbody2D _body;
        private Color _baseColor;
        private Coroutine _flashRoutine;

        public bool IsAlive { get { return currentHealth > 0f; } }

        void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _body = GetComponent<Rigidbody2D>();
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
            }

            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 1f, maxHealth);
        }

        public void TakeHit(float damage, Vector2 knockback, bool launch, bool slam)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
                _body.AddForce(knockback, ForceMode2D.Impulse);
                if (launch)
                {
                    _body.AddForce(Vector2.up * 8f, ForceMode2D.Impulse);
                }
                if (slam)
                {
                    _body.AddForce(Vector2.down * 12f, ForceMode2D.Impulse);
                }
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }
            _flashRoutine = StartCoroutine(Flash());

            if (currentHealth <= 0f)
            {
                if (destroyOnDeath)
                {
                    Destroy(gameObject, 0.05f);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator Flash()
        {
            if (_renderer == null)
            {
                yield break;
            }

            _renderer.color = Color.white;
            yield return new WaitForSeconds(hitFlashTime);
            _renderer.color = _baseColor;
        }
    }
}
