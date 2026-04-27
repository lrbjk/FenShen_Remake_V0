using System.Collections;
using UnityEngine;

namespace FenShen.Combat
{
    public class PooledObject : MonoBehaviour
    {
        private PooledObjectSpawner _owner;
        private Coroutine _releaseRoutine;

        public GameObject SourcePrefab { get; private set; }
        public bool IsSpawned { get; private set; }

        public void Initialize(PooledObjectSpawner owner, GameObject sourcePrefab)
        {
            _owner = owner;
            SourcePrefab = sourcePrefab;
        }

        public void OnSpawned(float lifetime)
        {
            IsSpawned = true;
            if (_releaseRoutine != null)
            {
                StopCoroutine(_releaseRoutine);
                _releaseRoutine = null;
            }

            SendMessage("OnPooledSpawned", SendMessageOptions.DontRequireReceiver);

            if (lifetime > 0f)
            {
                _releaseRoutine = StartCoroutine(ReleaseAfterDelay(lifetime));
            }
        }

        public void Release()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (_releaseRoutine != null)
            {
                StopCoroutine(_releaseRoutine);
                _releaseRoutine = null;
            }

            IsSpawned = false;
            SendMessage("OnPooledDespawned", SendMessageOptions.DontRequireReceiver);

            if (_owner != null)
            {
                _owner.Release(this);
                return;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator ReleaseAfterDelay(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            _releaseRoutine = null;
            Release();
        }
    }
}
