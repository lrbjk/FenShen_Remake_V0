using System.Collections;
using UnityEngine;

namespace FenShen.CombatPrototype
{
    public class CombatPrototypeEnemy : MonoBehaviour
    {
        public float moveSpeed = 3f;
        public float attackRange = 1.1f;
        public float attackCooldown = 1.2f;
        public float attackDamage = 10f;
        public Vector2 attackBox = new Vector2(1.2f, 1.0f);
        public Vector2 attackOffset = new Vector2(0.7f, 0.2f);

        private Rigidbody2D _body;
        private SpriteRenderer _renderer;
        private PrototypeDamageable _damageable;
        private float _nextAttackAt;
        private float _facing = -1f;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _damageable = GetComponent<PrototypeDamageable>();
        }

        void Update()
        {
            if (_damageable != null && !_damageable.IsAlive)
            {
                return;
            }

            Transform target = FindTarget();
            if (target == null)
            {
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                return;
            }

            float deltaX = target.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.05f)
            {
                _facing = Mathf.Sign(deltaX);
            }

            if (_renderer != null)
            {
                _renderer.flipX = _facing < 0f;
            }

            if (Mathf.Abs(deltaX) > attackRange)
            {
                _body.linearVelocity = new Vector2(_facing * moveSpeed, _body.linearVelocity.y);
            }
            else
            {
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                TryAttack();
            }
        }

        private Transform FindTarget()
        {
            CombatPrototypeRuntime player = CombatPrototypeRuntime.Instance;
            Transform best = player != null ? player.transform : null;
            float bestDistance = best != null ? Vector2.Distance(transform.position, best.position) : float.MaxValue;

            for (int i = CombatPrototypeDecoy.ActiveDecoys.Count - 1; i >= 0; i--)
            {
                CombatPrototypeDecoy decoy = CombatPrototypeDecoy.ActiveDecoys[i];
                if (decoy == null)
                {
                    CombatPrototypeDecoy.ActiveDecoys.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(transform.position, decoy.transform.position);
                if (distance < bestDistance + 3f)
                {
                    best = decoy.transform;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void TryAttack()
        {
            if (Time.time < _nextAttackAt)
            {
                return;
            }

            _nextAttackAt = Time.time + attackCooldown;
            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            if (_renderer != null)
            {
                _renderer.color = new Color(1f, 0.35f, 0.3f);
            }

            yield return new WaitForSeconds(0.18f);

            Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * _facing, attackOffset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBox, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable target = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (target == null || target.team == PrototypeTeam.Enemy)
                {
                    continue;
                }

                CombatPrototypeRuntime player = target.GetComponent<CombatPrototypeRuntime>();
                if (player != null && player.TryPerfectDodgeWindow())
                {
                    continue;
                }

                target.TakeHit(attackDamage, new Vector2(_facing * 4f, 2f), false, false);
            }

            yield return new WaitForSeconds(0.12f);
            if (_renderer != null)
            {
                _renderer.color = new Color(0.95f, 0.24f, 0.26f);
            }
        }
    }
}
