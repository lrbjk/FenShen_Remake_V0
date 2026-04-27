using UnityEngine;

namespace FenShen.Combat
{
    [RequireComponent(typeof(PooledObject))]
    public class SimplePooledProjectile : MonoBehaviour
    {
        private PooledObject _pooledObject;
        private ProjectileRuntimeContext _context;
        private bool _initialized;

        void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
        }

        void Update()
        {
            if (!_initialized || _context.Clip == null)
            {
                return;
            }

            transform.position += _context.Direction * (_context.Clip.speed * Time.deltaTime);
        }

        void OnPooledDespawned()
        {
            _initialized = false;
        }

        public void InitializeProjectile(ProjectileRuntimeContext context)
        {
            _context = context;
            _initialized = true;
        }

        public void Release()
        {
            if (_pooledObject != null)
            {
                _pooledObject.Release();
            }
        }
    }
}
