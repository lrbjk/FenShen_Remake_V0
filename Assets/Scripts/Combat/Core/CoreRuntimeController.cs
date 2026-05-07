using System.Collections.Generic;
using FenShen.GameData;
using FenShen.PlayerFSM;
using UnityEngine;

namespace FenShen.Combat
{
    public class CoreRuntimeController : MonoBehaviour
    {
        [Header("引用")]
        [InspectorName("玩家FSM")]
        [SerializeField] private PlayerFsm playerFsm;
        [InspectorName("武器运行时")]
        [SerializeField] private WeaponRuntimeController weaponRuntime;

        [Header("核心")]
        [InspectorName("当前核心")]
        [SerializeField] private CoreStyleDefinitionSO currentCore;

        private float _currentResource;
        private string _appliedModifierSourceId;
        private int _aerialDodgeCount;
        private int _empoweredAttackCharges;

        public CoreStyleDefinitionSO CurrentCore
        {
            get { return currentCore; }
        }

        public float CurrentResource
        {
            get { return _currentResource; }
        }

        public float MaxResource
        {
            get { return currentCore != null && currentCore.resource != null ? Mathf.Max(0f, currentCore.resource.maxResource) : 0f; }
        }

        void Awake()
        {
            if (playerFsm == null)
            {
                playerFsm = GetComponent<PlayerFsm>();
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponent<WeaponRuntimeController>();
            }

            ApplyCurrentCore();
        }

        void Update()
        {
            if (currentCore == null || currentCore.resource == null)
            {
                return;
            }

            if (playerFsm != null && playerFsm.CheckGround())
            {
                _aerialDodgeCount = 0;
            }

            if (currentCore.resource.passiveRegenPerSecond > 0f)
            {
                GainResource(currentCore.resource.passiveRegenPerSecond * Time.deltaTime);
            }
        }

        public void EquipCore(CoreStyleDefinitionSO core)
        {
            if (currentCore == core)
            {
                return;
            }

            RemovePassiveModifiers();
            currentCore = core;
            ApplyCurrentCore();
        }

        public bool CanPaySkillCost(CombatSkillDefinitionSO skill)
        {
            if (currentCore == null || currentCore.skillCost == null)
            {
                return true;
            }

            if (!currentCore.skillCost.requireEnoughResource)
            {
                return true;
            }

            return _currentResource >= ResolveSkillCost(skill);
        }

        public bool TryPaySkillCost(CombatSkillDefinitionSO skill)
        {
            float cost = ResolveSkillCost(skill);
            if (cost <= 0f)
            {
                return true;
            }

            if (_currentResource < cost)
            {
                return false;
            }

            _currentResource = Mathf.Max(0f, _currentResource - cost);
            return true;
        }

        public WeaponCancelPermission ModifyCancelPermission(WeaponCancelPermission basePermission)
        {
            if (currentCore == null || currentCore.cancelRule == null || !currentCore.cancelRule.overrideCancelPermission)
            {
                return basePermission;
            }

            if (currentCore.cancelRule.requireResourceThreshold && _currentResource < currentCore.cancelRule.minResource)
            {
                return basePermission;
            }

            return currentCore.cancelRule.cancelPermission;
        }

        public List<CombatSkillDefinitionSO> GetSpecialDerivations(CombatSkillDefinitionSO currentSkill, bool grounded, bool hasHitConfirm)
        {
            List<CombatSkillDefinitionSO> results = new List<CombatSkillDefinitionSO>();
            if (currentCore == null || currentCore.specialDerivations == null)
            {
                return results;
            }

            for (int i = 0; i < currentCore.specialDerivations.Count; i++)
            {
                CoreSpecialDerivationRule rule = currentCore.specialDerivations[i];
                if (!CanUseSpecialDerivation(rule, currentSkill, grounded, hasHitConfirm))
                {
                    continue;
                }

                if (rule.nextSkill != null && !results.Contains(rule.nextSkill))
                {
                    results.Add(rule.nextSkill);
                }
            }

            return results;
        }

        public bool TryConsumeSpecialDerivationCost(CombatSkillDefinitionSO currentSkill, CombatSkillDefinitionSO nextSkill, bool grounded, bool hasHitConfirm)
        {
            if (currentCore == null || currentCore.specialDerivations == null || nextSkill == null)
            {
                return true;
            }

            for (int i = 0; i < currentCore.specialDerivations.Count; i++)
            {
                CoreSpecialDerivationRule rule = currentCore.specialDerivations[i];
                if (rule == null || rule.nextSkill != nextSkill)
                {
                    continue;
                }

                if (!CanUseSpecialDerivation(rule, currentSkill, grounded, hasHitConfirm))
                {
                    continue;
                }

                if (!rule.consumeResourceOnEnter || rule.resourceCost <= 0f)
                {
                    return true;
                }

                if (_currentResource < rule.resourceCost)
                {
                    return false;
                }

                _currentResource = Mathf.Max(0f, _currentResource - rule.resourceCost);
                return true;
            }

            return true;
        }

        public void NotifySkillStarted(CombatSkillDefinitionSO skill, bool grounded)
        {
            GainFromRules(grounded ? CoreResourceGainTrigger.GroundedSkillStart : CoreResourceGainTrigger.AerialSkillStart, grounded);
            GainFromRules(CoreResourceGainTrigger.SkillStart, grounded);
        }

        public void NotifySkillFinished(CombatSkillDefinitionSO skill, bool grounded)
        {
            GainFromRules(CoreResourceGainTrigger.SkillEnd, grounded);
        }

        public void NotifyHitConfirmed(CombatSkillDefinitionSO skill, bool grounded)
        {
            GainFromRules(CoreResourceGainTrigger.HitConfirm, grounded);
        }

        public void NotifyDodge(bool perfect)
        {
            bool grounded = playerFsm == null || playerFsm.CheckGround();
            if (!grounded)
            {
                _aerialDodgeCount++;
            }

            GainFromRules(CoreResourceGainTrigger.Dodge, grounded);
            if (perfect)
            {
                GainFromRules(CoreResourceGainTrigger.PerfectDodge, grounded);
            }

            ApplyDodgeCombatRules(perfect);
        }

        public void NotifyGuardSuccess()
        {
            bool grounded = playerFsm == null || playerFsm.CheckGround();
            GainFromRules(CoreResourceGainTrigger.Guard, grounded);
        }

        public bool AllowsRepeatedAerialAttackBeforeLanding()
        {
            return currentCore != null
                && currentCore.airGroundRule != null
                && currentCore.airGroundRule.allowRepeatedAerialAttackBeforeLanding;
        }

        public bool CanEnterDodge(bool grounded)
        {
            if (grounded)
            {
                return true;
            }

            if (currentCore == null || currentCore.dodgeRule == null || !currentCore.dodgeRule.allowAerialDodge)
            {
                return false;
            }

            return _aerialDodgeCount < Mathf.Max(0, currentCore.dodgeRule.maxAerialDodgesBeforeLanding);
        }

        public float ModifyDamageDealt(float damage)
        {
            if (currentCore == null || currentCore.riskReward == null)
            {
                return damage;
            }

            float result = damage * Mathf.Max(0f, currentCore.riskReward.damageDealtMultiplier);
            if (_empoweredAttackCharges > 0 && currentCore.dodgeRule != null)
            {
                _empoweredAttackCharges--;
                result *= Mathf.Max(0f, currentCore.dodgeRule.empoweredAttackDamageMultiplier);
            }

            return result;
        }

        private void ApplyCurrentCore()
        {
            if (currentCore == null)
            {
                _currentResource = 0f;
                _aerialDodgeCount = 0;
                _empoweredAttackCharges = 0;
                return;
            }

            _currentResource = currentCore.resource != null
                ? Mathf.Clamp(currentCore.resource.initialResource, 0f, MaxResource)
                : 0f;
            _aerialDodgeCount = 0;
            _empoweredAttackCharges = 0;

            ApplyPassiveModifiers();
        }

        private void ApplyDodgeCombatRules(bool perfect)
        {
            if (currentCore == null || currentCore.dodgeRule == null)
            {
                return;
            }

            CoreDodgeRule rule = currentCore.dodgeRule;
            if (rule.refreshPrimaryAttackOnDodge && weaponRuntime != null)
            {
                weaponRuntime.ResetCombo();
            }

            if (rule.refreshAerialAttackLockOnDodge && playerFsm != null && playerFsm.CombatController != null)
            {
                playerFsm.CombatController.ClearAerialAttackLock();
            }

            if (!rule.empowerNextAttackOnDodge)
            {
                return;
            }

            if (rule.onlyEmpowerOnPerfectDodge && !perfect)
            {
                return;
            }

            _empoweredAttackCharges = Mathf.Max(_empoweredAttackCharges, 1);
        }

        private void ApplyPassiveModifiers()
        {
            if (playerFsm == null || playerFsm.RuntimeStats == null || playerFsm.RuntimeStats.Stats == null || currentCore == null)
            {
                return;
            }

            _appliedModifierSourceId = "core:" + currentCore.ResolveCoreId();
            if (currentCore.passiveRules == null)
            {
                return;
            }

            for (int i = 0; i < currentCore.passiveRules.Count; i++)
            {
                CorePassiveRule rule = currentCore.passiveRules[i];
                if (rule == null || rule.statModifiers == null)
                {
                    continue;
                }

                for (int j = 0; j < rule.statModifiers.Count; j++)
                {
                    StatModifierEntry modifier = rule.statModifiers[j];
                    if (modifier == null || string.IsNullOrWhiteSpace(modifier.statKey))
                    {
                        continue;
                    }

                    playerFsm.RuntimeStats.Stats.AddModifier(new RuntimeStatModifier
                    {
                        sourceId = _appliedModifierSourceId,
                        statKey = modifier.statKey,
                        operation = modifier.operation,
                        value = modifier.value
                    });
                }
            }
        }

        private void RemovePassiveModifiers()
        {
            if (playerFsm == null || playerFsm.RuntimeStats == null || playerFsm.RuntimeStats.Stats == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_appliedModifierSourceId))
            {
                playerFsm.RuntimeStats.Stats.RemoveModifiersBySource(_appliedModifierSourceId);
            }

            _appliedModifierSourceId = string.Empty;
        }

        private float ResolveSkillCost(CombatSkillDefinitionSO skill)
        {
            if (currentCore == null || currentCore.skillCost == null)
            {
                return 0f;
            }

            float cost = Mathf.Max(0f, currentCore.skillCost.flatResourceCost);
            if (currentCore.skillCost.useSkillResourceCost && skill != null)
            {
                cost += Mathf.Max(0f, skill.resourceCost);
            }

            return cost;
        }

        private void GainFromRules(CoreResourceGainTrigger trigger, bool grounded)
        {
            if (currentCore == null || currentCore.resourceGainRules == null)
            {
                return;
            }

            for (int i = 0; i < currentCore.resourceGainRules.Count; i++)
            {
                CoreResourceGainRule rule = currentCore.resourceGainRules[i];
                if (rule == null || rule.trigger != trigger)
                {
                    continue;
                }

                if (rule.requiresGrounded && !grounded)
                {
                    continue;
                }

                if (rule.requiresAerial && grounded)
                {
                    continue;
                }

                GainResource(ResolveGainAmount(rule, grounded));
            }
        }

        private float ResolveGainAmount(CoreResourceGainRule rule, bool grounded)
        {
            float amount = rule != null ? Mathf.Max(0f, rule.amount) : 0f;
            if (weaponRuntime != null && weaponRuntime.CurrentWeapon != null)
            {
                amount *= Mathf.Max(0f, weaponRuntime.CurrentWeapon.hitResourceGainMultiplier);
            }

            if (rule != null)
            {
                amount *= Mathf.Max(0f, rule.weaponGainMultiplier);
            }

            if (currentCore != null && currentCore.airGroundRule != null)
            {
                amount *= grounded
                    ? Mathf.Max(0f, currentCore.airGroundRule.groundedResourceGainMultiplier)
                    : Mathf.Max(0f, currentCore.airGroundRule.aerialResourceGainMultiplier);
            }

            if (currentCore != null && currentCore.riskReward != null)
            {
                amount *= Mathf.Max(0f, currentCore.riskReward.resourceGainMultiplier);
            }

            return amount;
        }

        private void GainResource(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            _currentResource = Mathf.Clamp(_currentResource + amount, 0f, MaxResource);
        }

        private bool CanUseSpecialDerivation(CoreSpecialDerivationRule rule, CombatSkillDefinitionSO currentSkill, bool grounded, bool hasHitConfirm)
        {
            if (rule == null || rule.nextSkill == null)
            {
                return false;
            }

            if (rule.fromSkill != null && rule.fromSkill != currentSkill)
            {
                return false;
            }

            if (rule.requiresHitConfirm && !hasHitConfirm)
            {
                return false;
            }

            if (rule.requiresGrounded && !grounded)
            {
                return false;
            }

            if (rule.requiresAerial && grounded)
            {
                return false;
            }

            if (rule.requireResourceThreshold && _currentResource < rule.minResource)
            {
                return false;
            }

            return true;
        }
    }
}
