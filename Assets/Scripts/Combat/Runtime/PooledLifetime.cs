using UnityEngine;

namespace FenShen.Combat
{
    [RequireComponent(typeof(PooledObject))]
    public class PooledLifetime : MonoBehaviour
    {
        [InspectorName("生命周期")]
        [Min(0f)] public float lifetime = 1f;
        [InspectorName("生成后自动回收")]
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
