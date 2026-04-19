using UnityEngine;

namespace FenShen.GameData
{
    public abstract class BuffBase : IBuff
    {
        protected BuffInstance instance;

        public BuffInstance Instance
        {
            get
            {
                return instance;
            }
        }

        public bool IsExpired
        {
            get
            {
                return instance == null || instance.IsExpired;
            }
        }

        public virtual void Initialize(BuffInstance buffInstance)
        {
            instance = buffInstance;
        }

        public virtual void OnApply()
        {
        }

        public virtual void OnRemove()
        {
        }

        public virtual void OnRefresh()
        {
        }

        public virtual void OnTick()
        {
        }

        public virtual void OnStackChanged(int previousStack, int newStack)
        {
        }

        public virtual void Update(float deltaTime)
        {
            if (instance == null || instance.data == null)
            {
                return;
            }

            if (instance.HasDuration)
            {
                instance.remainingTime -= deltaTime;
            }

            if (instance.data.tickInterval <= 0f)
            {
                return;
            }

            instance.tickTimer += deltaTime;
            while (instance.tickTimer >= instance.data.tickInterval)
            {
                instance.tickTimer -= instance.data.tickInterval;
                OnTick();
            }
        }

        protected RuntimeStats GetRuntimeStats()
        {
            return instance != null && instance.ownerStats != null ? instance.ownerStats.Stats : null;
        }

        protected int GetCurrentStack()
        {
            return instance != null ? Mathf.Max(1, instance.stack) : 1;
        }
    }
}
