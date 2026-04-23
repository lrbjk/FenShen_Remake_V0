using FenShen.Combat;
using UnityEngine;
namespace FenShen.PlayerFSM
{
    public class SimpleCombatSkillSystem : MonoBehaviour, ICombatSkillSystem
    {
        [SerializeField] private CombatCoordinator combatCoordinator;
        [SerializeField] private bool simulateHitConfirmOnExecute;

        void Awake()
        {
            if (combatCoordinator == null)
            {
                combatCoordinator = GetComponent<CombatCoordinator>();
            }
        }

        public void ExecuteAttack(FenShen.Combat.CombatSkillDefinitionSO skill)
        {
            if (skill == null)
            {
                Debug.Log("[Combat] ExecuteAttack: <null skill>");
                return;
            }

            Debug.Log("[Combat] ExecuteAttack Asset: " + skill.DisplayName + " (" + skill.ResolveSkillId() + ")");
            if (simulateHitConfirmOnExecute && combatCoordinator != null)
            {
                combatCoordinator.NotifySkillHitConfirmed(skill);
            }
        }

        public void ExecuteAttack(string skillId)
        {
            Debug.Log("[Combat] ExecuteAttack: " + skillId);
            if (simulateHitConfirmOnExecute && combatCoordinator != null && !string.IsNullOrWhiteSpace(skillId))
            {
                combatCoordinator.NotifySkillHitConfirmed(skillId);
            }
        }
    }
}
