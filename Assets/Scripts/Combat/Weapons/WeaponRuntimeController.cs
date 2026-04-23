using FenShen.GameData;
using FenShen.PlayerFSM;
using UnityEngine;

namespace FenShen.Combat
{
    public class WeaponRuntimeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerFsm playerFsm;

        [Header("Weapon")]
        [SerializeField] private WeaponDefinitionSO currentWeapon;
        [SerializeField] private bool resetComboOnWeaponSwap = true;

        [Header("Combo")]
        [SerializeField] private float comboResetDelay = 0.9f;
        [SerializeField] private bool useRuntimeComboWindowStat = true;
        [SerializeField] private float fallbackComboWindow = 0.2f;

        private int _groundPrimaryComboIndex;
        private int _airPrimaryComboIndex;
        private float _comboExpireTime = float.NegativeInfinity;
        private string _lastResolvedSkillId;
        private CombatSkillDefinitionSO _lastResolvedSkill;

        public WeaponDefinitionSO CurrentWeapon
        {
            get { return currentWeapon; }
        }

        public CombatSkillDefinitionSO LastResolvedSkill
        {
            get { return _lastResolvedSkill; }
        }

        void Awake()
        {
            if (playerFsm == null)
            {
                playerFsm = GetComponent<PlayerFsm>();
            }
        }

        void Update()
        {
            if (Time.time > _comboExpireTime)
            {
                ResetCombo();
            }
        }

        public void EquipWeapon(WeaponDefinitionSO weapon)
        {
            if (currentWeapon == weapon)
            {
                return;
            }

            currentWeapon = weapon;
            if (resetComboOnWeaponSwap)
            {
                ResetCombo();
            }
        }

        public CombatSkillDefinitionSO ResolvePrimaryAttackSkillAsset(bool grounded)
        {
            if (currentWeapon == null)
            {
                return null;
            }

            WeaponMoveSetSO moveSet = currentWeapon.moveSet;
            if (moveSet != null)
            {
                CombatSkillDefinitionSO comboSkill = moveSet.GetPrimaryComboSkillAsset(
                    grounded ? _groundPrimaryComboIndex : _airPrimaryComboIndex,
                    grounded);
                if (comboSkill != null)
                {
                    return comboSkill;
                }
            }

            return currentWeapon.GetEntrySkillAsset(grounded ? WeaponAttackSlot.PrimaryGround : WeaponAttackSlot.PrimaryAir);
        }

        public string ResolvePrimaryAttackSkillId(bool grounded)
        {
            if (currentWeapon == null)
            {
                return string.Empty;
            }

            WeaponMoveSetSO moveSet = currentWeapon.moveSet;
            if (moveSet != null)
            {
                string comboSkillId = moveSet.GetPrimaryComboSkillId(
                    grounded ? _groundPrimaryComboIndex : _airPrimaryComboIndex,
                    grounded);
                if (!string.IsNullOrWhiteSpace(comboSkillId))
                {
                    return comboSkillId;
                }
            }

            return currentWeapon.GetEntrySkillId(grounded ? WeaponAttackSlot.PrimaryGround : WeaponAttackSlot.PrimaryAir);
        }

        public CombatSkillDefinitionSO ResolveSkillAssetForSlot(WeaponAttackSlot slot, bool grounded)
        {
            if (currentWeapon == null)
            {
                return null;
            }

            if (slot == WeaponAttackSlot.PrimaryGround || slot == WeaponAttackSlot.PrimaryAir)
            {
                return ResolvePrimaryAttackSkillAsset(grounded);
            }

            return currentWeapon.GetEntrySkillAsset(slot);
        }

        public string ResolveSkillIdForSlot(WeaponAttackSlot slot, bool grounded)
        {
            if (currentWeapon == null)
            {
                return string.Empty;
            }

            if (slot == WeaponAttackSlot.PrimaryGround || slot == WeaponAttackSlot.PrimaryAir)
            {
                return ResolvePrimaryAttackSkillId(grounded);
            }

            return currentWeapon.GetEntrySkillId(slot);
        }

        public void NotifyAttackStarted(CombatSkillDefinitionSO skill, string skillId, bool grounded)
        {
            if (skill == null && string.IsNullOrWhiteSpace(skillId))
            {
                return;
            }

            _lastResolvedSkill = skill;
            _lastResolvedSkillId = skillId;
            AdvancePrimaryCombo(grounded, skill, skillId);
            _comboExpireTime = Time.time + GetResolvedComboResetDelay();
        }

        public void NotifyCombatFinished()
        {
            _comboExpireTime = Time.time + GetResolvedComboResetDelay();
        }

        public void ResetCombo()
        {
            _groundPrimaryComboIndex = 0;
            _airPrimaryComboIndex = 0;
            _comboExpireTime = float.PositiveInfinity;
            _lastResolvedSkill = null;
            _lastResolvedSkillId = string.Empty;
        }

        private void AdvancePrimaryCombo(bool grounded, CombatSkillDefinitionSO resolvedSkill, string resolvedSkillId)
        {
            if (currentWeapon == null || currentWeapon.moveSet == null)
            {
                return;
            }

            WeaponMoveSetSO moveSet = currentWeapon.moveSet;
            if (grounded)
            {
                int nextIndex = _groundPrimaryComboIndex + 1;
                bool hasNextSkill = moveSet.GetPrimaryComboSkillAsset(nextIndex, true) != null
                    || !string.IsNullOrWhiteSpace(moveSet.GetPrimaryComboSkillId(nextIndex, true));
                _groundPrimaryComboIndex = hasNextSkill ? nextIndex : 0;
                _airPrimaryComboIndex = 0;
                return;
            }

            int nextAirIndex = _airPrimaryComboIndex + 1;
            bool hasNextAirSkill = moveSet.GetPrimaryComboSkillAsset(nextAirIndex, false) != null
                || !string.IsNullOrWhiteSpace(moveSet.GetPrimaryComboSkillId(nextAirIndex, false));
            _airPrimaryComboIndex = hasNextAirSkill ? nextAirIndex : 0;
            _groundPrimaryComboIndex = 0;
        }

        private float GetResolvedComboResetDelay()
        {
            float delay = comboResetDelay;
            if (playerFsm != null && useRuntimeComboWindowStat)
            {
                float runtimeComboWindow = playerFsm.GetStat(StatKeys.ComboWindow, fallbackComboWindow);
                delay = Mathf.Max(delay, runtimeComboWindow);
            }

            return Mathf.Max(0.05f, delay);
        }
    }
}
