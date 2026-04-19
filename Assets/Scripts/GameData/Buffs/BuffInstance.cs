using System;
using UnityEngine;

namespace FenShen.GameData
{
    [Serializable]
    public class BuffInstance
    {
        public BuffSO data;
        public BuffController controller;
        public RuntimeStatsComponent ownerStats;
        public GameObject source;
        public float remainingTime;
        public float tickTimer;
        public int stack;
        public string sourceId;

        public int BuffId
        {
            get
            {
                return data != null ? data.RuntimeKey : 0;
            }
        }

        public bool HasDuration
        {
            get
            {
                return data != null && data.duration > 0f;
            }
        }

        public bool IsExpired
        {
            get
            {
                return HasDuration && remainingTime <= 0f;
            }
        }

        public void RefreshDuration()
        {
            if (data == null)
            {
                return;
            }

            remainingTime = data.duration;
        }
    }
}
