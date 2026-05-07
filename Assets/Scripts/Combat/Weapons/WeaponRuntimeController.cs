using FenShen.GameData;
using FenShen.PlayerFSM;
using UnityEngine;

namespace FenShen.Combat
{
    public class WeaponRuntimeController : MonoBehaviour
    {
        [Header("引用")]
        [InspectorName("玩家FSM")]
        [SerializeField] private PlayerFsm playerFsm;

        [Header("武器")]
        [InspectorName("当前武器")]
        [SerializeField] private WeaponDefinitionSO currentWeapon;
        [InspectorName("切换武器时重置连段")]
        [SerializeField] private bool resetComboOnWeaponSwap = true;

        [Header("连段")]
        [InspectorName("连段重置延迟")]
        [SerializeField] private float comboResetDelay = 0.9f;
        [InspectorName("使用运行时连段窗口属性")]
        [SerializeField] private bool useRuntimeComboWindowStat = true;
        [InspectorName("备用连段窗口")]
        [SerializeField] private float fallbackComboWindow = 0.2f;

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

            return currentWeapon.GetEntrySkillAsset(grounded ? WeaponAttackSlot.PrimaryGround : WeaponAttackSlot.PrimaryAir);
        }

        public string ResolvePrimaryAttackSkillId(bool grounded)
        {
            if (currentWeapon == null)
            {
                return string.Empty;
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
            _comboExpireTime = Time.time + GetResolvedComboResetDelay();
        }

        public void NotifyCombatFinished()
        {
            _comboExpireTime = Time.time + GetResolvedComboResetDelay();
        }

        public void ResetCombo()
        {
            _comboExpireTime = float.PositiveInfinity;
            _lastResolvedSkill = null;
            _lastResolvedSkillId = string.Empty;
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
