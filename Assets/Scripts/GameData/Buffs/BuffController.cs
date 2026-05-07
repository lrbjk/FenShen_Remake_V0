using System;
using System.Collections.Generic;
using FenShen.Combat;
using UnityEngine;

namespace FenShen.GameData
{
    public class BuffController : MonoBehaviour, ICombatBuffReceiver
    {
        private readonly Dictionary<int, BuffBase> activeBuffs = new Dictionary<int, BuffBase>();
        private readonly List<BuffBase> updateOrder = new List<BuffBase>();
        private readonly Dictionary<BuffControlFlag, int> controlFlagCounts = new Dictionary<BuffControlFlag, int>();
        private static BuffDatabaseSO cachedDefaultDatabase;

        [SerializeField] private RuntimeStatsComponent statsComponent;
        [SerializeField] private BuffDatabaseSO buffDatabase;

        public event Action<BuffBase> BuffApplied;
        public event Action<BuffBase> BuffRemoved;

        public RuntimeStatsComponent StatsComponent
        {
            get
            {
                return statsComponent;
            }
        }

        public BuffDatabaseSO BuffDatabase
        {
            get
            {
                return buffDatabase;
            }
            set
            {
                buffDatabase = value;
            }
        }

        private void Awake()
        {
            if (statsComponent == null)
            {
                statsComponent = GetComponent<RuntimeStatsComponent>();
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = updateOrder.Count - 1; i >= 0; i--)
            {
                BuffBase buff = updateOrder[i];
                if (buff == null)
                {
                    updateOrder.RemoveAt(i);
                    continue;
                }

                buff.Update(deltaTime);
                if (buff.IsExpired)
                {
                    RemoveBuffInternal(buff.Instance.BuffId);
                }
            }
        }

        public BuffBase AddBuff(BuffSO data, GameObject source = null)
        {
            if (data == null)
            {
                return null;
            }

            int buffId = data.RuntimeKey;
            if (activeBuffs.TryGetValue(buffId, out BuffBase existing))
            {
                return HandleReapply(existing, data, source);
            }

            BuffBase buff = BuffFactory.Create(data);
            if (buff == null)
            {
                return null;
            }

            BuffInstance instance = new BuffInstance
            {
                data = data,
                controller = this,
                ownerStats = statsComponent,
                source = source,
                remainingTime = data.duration,
                tickTimer = 0f,
                stack = 1,
                sourceId = "buff:" + buffId
            };

            buff.Initialize(instance);
            activeBuffs[buffId] = buff;
            updateOrder.Add(buff);
            buff.OnApply();
            BuffApplied?.Invoke(buff);
            return buff;
        }

        public BuffBase AddBuff(int buffId, GameObject source = null)
        {
            if (!TryResolveBuff(buffId, out BuffSO buff))
            {
                Debug.LogWarning($"BuffController could not resolve buff id '{buffId}' on {name}.", this);
                return null;
            }

            return AddBuff(buff, source);
        }

        public bool RemoveBuff(BuffSO data)
        {
            return data != null && RemoveBuff(data.RuntimeKey);
        }

        public bool RemoveBuff(int buffId)
        {
            return RemoveBuffInternal(buffId);
        }

        public bool HasBuff(int buffId)
        {
            return buffId > 0 && activeBuffs.ContainsKey(buffId);
        }

        public bool TryGetBuff(int buffId, out BuffBase buff)
        {
            return activeBuffs.TryGetValue(buffId, out buff);
        }

        public bool HasControlFlag(BuffControlFlag flag)
        {
            return flag != BuffControlFlag.None &&
                   controlFlagCounts.TryGetValue(flag, out int count) &&
                   count > 0;
        }

        public void AdjustControlFlags(BuffControlFlag flags, int delta)
        {
            if (flags == BuffControlFlag.None || delta == 0)
            {
                return;
            }

            foreach (BuffControlFlag singleFlag in Enum.GetValues(typeof(BuffControlFlag)))
            {
                if (singleFlag == BuffControlFlag.None || !flags.HasFlag(singleFlag))
                {
                    continue;
                }

                controlFlagCounts.TryGetValue(singleFlag, out int current);
                current += delta;

                if (current <= 0)
                {
                    controlFlagCounts.Remove(singleFlag);
                }
                else
                {
                    controlFlagCounts[singleFlag] = current;
                }
            }
        }

        private bool TryResolveBuff(int buffId, out BuffSO buff)
        {
            buff = null;
            if (buffId <= 0)
            {
                return false;
            }

            if (buffDatabase != null && buffDatabase.TryGetBuff(buffId, out buff))
            {
                return true;
            }

            BuffDatabaseSO defaultDatabase = GetDefaultDatabase();
            return defaultDatabase != null && defaultDatabase.TryGetBuff(buffId, out buff);
        }

        private static BuffDatabaseSO GetDefaultDatabase()
        {
            if (cachedDefaultDatabase == null)
            {
                cachedDefaultDatabase = Resources.Load<BuffDatabaseSO>("GameData/BuffDatabase");
            }

            return cachedDefaultDatabase;
        }

        private BuffBase HandleReapply(BuffBase existing, BuffSO data, GameObject source)
        {
            BuffStackRule rule = data.ResolveStackRule();
            switch (rule)
            {
                case BuffStackRule.Ignore:
                    return existing;

                case BuffStackRule.Replace:
                    RemoveBuffInternal(existing.Instance.BuffId);
                    return AddBuff(data, source);

                case BuffStackRule.Stack:
                    ApplyStack(existing, data);
                    return existing;

                case BuffStackRule.Refresh:
                default:
                    if (data.refreshDurationOnReapply)
                    {
                        existing.Instance.RefreshDuration();
                    }

                    existing.OnRefresh();
                    return existing;
            }
        }

        private void ApplyStack(BuffBase existing, BuffSO data)
        {
            BuffInstance instance = existing.Instance;
            int previousStack = instance.stack;
            instance.stack = Mathf.Min(data.MaxStacks, instance.stack + 1);

            if (data.refreshDurationOnReapply)
            {
                instance.RefreshDuration();
            }

            if (instance.stack != previousStack)
            {
                existing.OnStackChanged(previousStack, instance.stack);
            }
            else
            {
                existing.OnRefresh();
            }
        }

        private bool RemoveBuffInternal(int buffId)
        {
            if (buffId <= 0 || !activeBuffs.TryGetValue(buffId, out BuffBase buff))
            {
                return false;
            }

            buff.OnRemove();
            activeBuffs.Remove(buffId);
            updateOrder.Remove(buff);
            BuffRemoved?.Invoke(buff);
            return true;
        }
    }
}
