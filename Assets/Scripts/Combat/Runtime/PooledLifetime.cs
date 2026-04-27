using UnityEngine;

namespace FenShen.Combat
{
    [RequireComponent(typeof(PooledObject))]
    public class PooledLifetime : MonoBehaviour
    {
        [Min(0f)] public float lifetime = 1f;
        [SerializeField] private bool releaseOnSpawn = true;

        private PooledObject _pooledObject;

        void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
        }

        void OnPooledSpawned()
        {
            if (releaseOnSpawn && _pooledObject != null && lifetime > 0f)
            {
                CancelInvoke(nameof(Release));
                Invoke(nameof(Release), lifetime);
            }
        }

        void OnPooledDespawned()
        {
            CancelInvoke(nameof(Release));
        }

        private void Release()
        {
            if (_pooledObject != null)
            {
                _pooledObject.Release();
            }
        }
    }
}
