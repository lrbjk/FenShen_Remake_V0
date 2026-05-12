using UnityEngine;

namespace FenShen.PlayerFSM
{
    public class PlayerCloneDecoyTarget : MonoBehaviour
    {
        [InspectorName("失效时间")]
        [SerializeField] private float expireTime;

        public bool IsActive
        {
            get { return gameObject.activeInHierarchy && Time.time < expireTime; }
        }

        public float RemainingTime
        {
            get { return Mathf.Max(0f, expireTime - Time.time); }
        }

        public void Initialize(float absoluteExpireTime)
        {
            expireTime = absoluteExpireTime;
            enabled = true;
        }

        public void Clear()
        {
            expireTime = 0f;
            enabled = false;
        }

        void Update()
        {
            if (Time.time >= expireTime)
            {
                Clear();
            }
        }
    }
}
