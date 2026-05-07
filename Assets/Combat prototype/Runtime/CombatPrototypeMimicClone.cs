using System.Collections;
using UnityEngine;

namespace FenShen.CombatPrototype
{
    public class CombatPrototypeMimicClone : MonoBehaviour
    {
        private CombatPrototypeRuntime _owner;
        private float _dieAt;

        public void Initialize(CombatPrototypeRuntime owner, float lifetime)
        {
            _owner = owner;
            _dieAt = Time.time + lifetime;
        }

        void Update()
        {
            if (Time.time >= _dieAt || _owner == null)
            {
                Destroy(gameObject);
            }
        }

        public void MimicAttack(Vector2 box, Vector2 offset, float damage, Vector2 knockback, bool launch, bool slam)
        {
            StartCoroutine(MimicAttackRoutine(box, offset, damage, knockback, launch, slam));
        }

        private IEnumerator MimicAttackRoutine(Vector2 box, Vector2 offset, float damage, Vector2 knockback, bool launch, bool slam)
        {
            yield return new WaitForSeconds(0.08f);
            PrototypeDamageable target = CombatPrototypeFactory.FindNearestEnemy(transform.position);
            float facing = target != null ? Mathf.Sign(target.transform.position.x - transform.position.x) : 1f;
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * facing, offset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, box, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable damageable = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (damageable == null || damageable.team != PrototypeTeam.Enemy)
                {
                    continue;
                }

                damageable.TakeHit(damage, new Vector2(facing * Mathf.Abs(knockback.x), knockback.y), launch, slam);
            }
        }
    }
}
