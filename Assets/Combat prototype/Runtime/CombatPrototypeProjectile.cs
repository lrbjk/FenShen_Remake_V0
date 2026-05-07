using UnityEngine;

namespace FenShen.CombatPrototype
{
    public class CombatPrototypeProjectile : MonoBehaviour
    {
        private PrototypeTeam _team;
        private Vector2 _direction;
        private float _speed;
        private float _damage;
        private float _dieAt;
        private bool _lift;

        public void Initialize(PrototypeTeam team, Vector2 direction, float speed, float lifetime, float damage, bool lift)
        {
            _team = team;
            _direction = direction.normalized;
            _speed = speed;
            _dieAt = Time.time + lifetime;
            _damage = damage;
            _lift = lift;
        }

        void Update()
        {
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
                return;
            }

            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(0.9f, 0.4f), 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable target = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (target == null || target.team == _team)
                {
                    continue;
                }

                target.TakeHit(_damage, _lift ? new Vector2(_direction.x * 2f, 5f) : new Vector2(_direction.x * 4f, 1f), _lift, false);
                Destroy(gameObject);
                return;
            }
        }
    }
}
