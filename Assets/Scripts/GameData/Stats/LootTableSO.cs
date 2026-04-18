using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "LootTable", menuName = "Game Data/Loot/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        public int version = 1;
        [Min(0)] public int minRolls = 1;
        [Min(0)] public int maxRolls = 1;
        public bool allowDuplicateDrops = true;
        public List<LootEntry> entries = new List<LootEntry>();
    }
}
