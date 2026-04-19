using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "BuffDatabase", menuName = "Game Data/Combat/Buff Database")]
    public class BuffDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<BuffSO> buffs = new List<BuffSO>();

        private Dictionary<int, BuffSO> lookup;

        public IReadOnlyList<BuffSO> Buffs
        {
            get
            {
                return buffs;
            }
        }

        public bool TryGetBuff(int buffId, out BuffSO buff)
        {
            EnsureLookup();
            return lookup.TryGetValue(buffId, out buff);
        }

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<int, BuffSO>();
            for (int i = 0; i < buffs.Count; i++)
            {
                BuffSO entry = buffs[i];
                if (entry == null)
                {
                    continue;
                }

                int key = entry.RuntimeKey;
                if (key <= 0 || lookup.ContainsKey(key))
                {
                    continue;
                }

                lookup.Add(key, entry);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lookup = null;
        }
#endif
    }
}
