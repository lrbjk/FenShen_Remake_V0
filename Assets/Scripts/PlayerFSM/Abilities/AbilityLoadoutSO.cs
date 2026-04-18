using System.Collections.Generic;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "AbilityLoadout", menuName = "PlayerFSM/Abilities/Ability Loadout")]
    public class AbilityLoadoutSO : ScriptableObject
    {
        public int version = 1;
        public List<AbilityDefinitionSO> abilities = new List<AbilityDefinitionSO>();
    }
}
