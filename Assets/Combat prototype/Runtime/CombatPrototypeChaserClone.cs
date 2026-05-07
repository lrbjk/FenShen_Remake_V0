using UnityEngine;

namespace FenShen.CombatPrototype
{
    public class CombatPrototypeChaserClone : MonoBehaviour
    {
        public float lifetime = 1.8f;
        public float speed = 11f;
        public float attackInterval = 0.2f;

        private CombatPrototypeRuntime _owner;
        private PrototypeDamageable _target;
        private float _dieAt;
        private float _nextAttackAt;

        public void Initialize(CombatPrototypeRuntime owner, PrototypeDamageable target)
        {
            _owner = owner;
            _target = target;
            _dieAt = Time.time + lifetime;
        }

        void Update()
        {
            if (Time.time >= _dieAt || _owner == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_target == null || !_target.IsAlive)
            {
                _target = CombatPrototypeFactory.FindNearestEnemy(transform.position);
            }

            if (_target != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, speed * Time.deltaTime);
                if (Vector2.Distance(transform.position, _target.transform.position) < 0.9f && Time.time >= _nextAttackAt)
                {
                    _nextAttackAt = Time.time + attackInterval;
                    float dir = Mathf.Sign(_target.transform.position.x - transform.position.x);
                    _target.TakeHit(7f, new Vector2(dir * 2.5f, 1.2f), false, false);
                }
            }
            else
            {
                transform.position += Vector3.right * speed * Time.deltaTime;
            }
        }
    }
}
