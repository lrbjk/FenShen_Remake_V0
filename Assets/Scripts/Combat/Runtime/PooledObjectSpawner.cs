using System.Collections.Generic;
using UnityEngine;

namespace FenShen.Combat
{
    public class PooledObjectSpawner : MonoBehaviour
    {
        private sealed class Pool
        {
            public readonly GameObject Prefab;
            public readonly Queue<PooledObject> Available = new Queue<PooledObject>();
            public readonly HashSet<PooledObject> Active = new HashSet<PooledObject>();

            public Pool(GameObject prefab)
            {
                Prefab = prefab;
            }
        }

        [Header("对象池")]
        [InspectorName("对象池根节点")]
        [SerializeField] private Transform poolRoot;
        [InspectorName("默认预热数量")]
        [SerializeField] private int defaultPrewarmCount;
        [InspectorName("每个预制体最大实例数")]
        [SerializeField] private int maxInstancesPerPrefab = 64;
        [InspectorName("允许扩容")]
        [SerializeField] private bool allowExpand = true;

        private readonly Dictionary<GameObject, Pool> _pools = new Dictionary<GameObject, Pool>();

        public static PooledObjectSpawner Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple PooledObjectSpawner instances found. The newest one will be used.", this);
            }

            Instance = this;
            if (poolRoot == null)
            {
                GameObject root = new GameObject("Pooled Objects");
                root.transform.SetParent(transform, false);
                poolRoot = root.transform;
            }
        }

        public static PooledObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 0f, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            PooledObjectSpawner spawner = ResolveInstance();
            return spawner.SpawnInstance(prefab, position, rotation, lifetime, parent);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Pool pool = GetOrCreatePool(prefab);
            int targetCount = Mathf.Min(count, Mathf.Max(1, maxInstancesPerPrefab));
            for (int i = pool.Available.Count + pool.Active.Count; i < targetCount; i++)
            {
                PooledObject instance = CreateInstance(pool);
                if (instance != null)
                {
                    pool.Available.Enqueue(instance);
                }
            }
        }

        public void Release(PooledObject pooledObject)
        {
            if (pooledObject == null || pooledObject.SourcePrefab == null)
            {
                return;
            }

            Pool pool = GetOrCreatePool(pooledObject.SourcePrefab);
            pool.Active.Remove(pooledObject);
            pooledObject.transform.SetParent(poolRoot, false);
            pooledObject.gameObject.SetActive(false);
            pool.Available.Enqueue(pooledObject);
        }

        private PooledObject SpawnInstance(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime, Transform parent)
        {
            Pool pool = GetOrCreatePool(prefab);
            PooledObject instance = GetAvailableInstance(pool);
            if (instance == null)
            {
                return null;
            }

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, true);
            instanceTransform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            pool.Active.Add(instance);
            instance.OnSpawned(lifetime);
            return instance;
        }

        private PooledObject GetAvailableInstance(Pool pool)
        {
            while (pool.Available.Count > 0)
            {
                PooledObject candidate = pool.Available.Dequeue();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            if (!allowExpand && pool.Active.Count >= Mathf.Max(1, maxInstancesPerPrefab))
            {
                return null;
            }

            if (pool.Active.Count >= Mathf.Max(1, maxInstancesPerPrefab) && maxInstancesPerPrefab > 0)
            {
                return null;
            }

            return CreateInstance(pool);
        }

        private Pool GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out Pool pool))
            {
                return pool;
            }

            pool = new Pool(prefab);
            _pools.Add(prefab, pool);
            if (defaultPrewarmCount > 0)
            {
                int targetCount = Mathf.Min(defaultPrewarmCount, Mathf.Max(1, maxInstancesPerPrefab));
                for (int i = 0; i < targetCount; i++)
                {
                    PooledObject instance = CreateInstance(pool);
                    if (instance != null)
                    {
                        pool.Available.Enqueue(instance);
                    }
                }
            }

            return pool;
        }

        private PooledObject CreateInstance(Pool pool)
        {
            if (pool == null || pool.Prefab == null)
            {
                return null;
            }

            GameObject instanceObject = Instantiate(pool.Prefab, poolRoot);
            instanceObject.name = pool.Prefab.name + " (Pooled)";
            instanceObject.SetActive(false);

            PooledObject pooledObject = instanceObject.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                pooledObject = instanceObject.AddComponent<PooledObject>();
            }

            pooledObject.Initialize(this, pool.Prefab);
            return pooledObject;
        }

        private static PooledObjectSpawner ResolveInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            PooledObjectSpawner existing = FindObjectOfType<PooledObjectSpawner>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject spawnerObject = new GameObject("Pooled Object Spawner");
            Instance = spawnerObject.AddComponent<PooledObjectSpawner>();
            return Instance;
        }
    }
}
