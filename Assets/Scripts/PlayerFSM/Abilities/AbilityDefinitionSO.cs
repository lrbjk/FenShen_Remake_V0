using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "AbilityDefinition", menuName = "PlayerFSM/Abilities/Ability Definition")]
    public class AbilityDefinitionSO : ScriptableObject
    {
        public AbilityId id = AbilityId.None;
        public string displayName;
        [TextArea]
        public string description;
        public Sprite icon;
        public bool unlockedByDefault;
        [Min(1)]
        public int defaultLevel = 1;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                return id.ToString();
            }
        }
    }
}
