using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FenShen.GameData
{
    public class BuffSystemTestHarness : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private BuffController target;
        [SerializeField] private bool useSelfIfTargetMissing = true;

        [Header("Buff Ids")]
        [SerializeField] private int applyBuffId = BuffIds.Poison;
        [SerializeField] private int removeBuffId = BuffIds.Poison;
        [SerializeField] private int queryBuffId = BuffIds.Poison;

        [Header("Control Query")]
        [SerializeField] private BuffControlFlag queryControlFlag = BuffControlFlag.Stun;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode applyKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode removeKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode queryBuffKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode queryControlKey = KeyCode.Alpha4;

        private void Awake()
        {
            ResolveTarget();
        }

        private void OnEnable()
        {
            ResolveTarget();
            SubscribeEvents(true);
        }

        private void OnDisable()
        {
            SubscribeEvents(false);
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            if (WasKeyPressedThisFrame(applyKey))
            {
                ApplyConfiguredBuff();
            }

            if (WasKeyPressedThisFrame(removeKey))
            {
                RemoveConfiguredBuff();
            }

            if (WasKeyPressedThisFrame(queryBuffKey))
            {
                LogBuffQuery();
            }

            if (WasKeyPressedThisFrame(queryControlKey))
            {
                LogControlQuery();
            }
        }

        [ContextMenu("Apply Configured Buff")]
        public void ApplyConfiguredBuff()
        {
            if (target == null)
            {
                Debug.LogWarning("BuffSystemTestHarness could not find a BuffController target.", this);
                return;
            }

            BuffBase buff = target.AddBuff(applyBuffId, gameObject);
            if (buff == null)
            {
                Debug.LogWarning($"Apply buff failed. buffId={applyBuffId}", this);
                return;
            }

            Debug.Log(
                $"Applied buff id={applyBuffId} to {target.name}. stack={buff.Instance.stack}, remain={buff.Instance.remainingTime:0.00}",
                this);
        }

        [ContextMenu("Remove Configured Buff")]
        public void RemoveConfiguredBuff()
        {
            if (target == null)
            {
                Debug.LogWarning("BuffSystemTestHarness could not find a BuffController target.", this);
                return;
            }

            bool removed = target.RemoveBuff(removeBuffId);
            Debug.Log($"Remove buff id={removeBuffId} on {target.name}: {removed}", this);
        }

        [ContextMenu("Log Buff Query")]
        public void LogBuffQuery()
        {
            if (target == null)
            {
                Debug.LogWarning("BuffSystemTestHarness could not find a BuffController target.", this);
                return;
            }

            bool hasBuff = target.HasBuff(queryBuffId);
            Debug.Log($"Query buff id={queryBuffId} on {target.name}: {hasBuff}", this);
        }

        [ContextMenu("Log Control Query")]
        public void LogControlQuery()
        {
            if (target == null)
            {
                Debug.LogWarning("BuffSystemTestHarness could not find a BuffController target.", this);
                return;
            }

            bool hasControl = target.HasControlFlag(queryControlFlag);
            Debug.Log($"Query control flag={queryControlFlag} on {target.name}: {hasControl}", this);
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            if (!useSelfIfTargetMissing)
            {
                return;
            }

            target = GetComponent<BuffController>();
        }

        private void SubscribeEvents(bool subscribe)
        {
            if (target == null)
            {
                return;
            }

            if (subscribe)
            {
                target.BuffApplied += OnBuffApplied;
                target.BuffRemoved += OnBuffRemoved;
            }
            else
            {
                target.BuffApplied -= OnBuffApplied;
                target.BuffRemoved -= OnBuffRemoved;
            }
        }

        private bool WasKeyPressedThisFrame(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Key mappedKey = MapToInputSystemKey(key);
                if (mappedKey != Key.None)
                {
                    return keyboard[mappedKey].wasPressedThisFrame;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private Key MapToInputSystemKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.Alpha8: return Key.Digit8;
                case KeyCode.Alpha9: return Key.Digit9;
                case KeyCode.Keypad0: return Key.Numpad0;
                case KeyCode.Keypad1: return Key.Numpad1;
                case KeyCode.Keypad2: return Key.Numpad2;
                case KeyCode.Keypad3: return Key.Numpad3;
                case KeyCode.Keypad4: return Key.Numpad4;
                case KeyCode.Keypad5: return Key.Numpad5;
                case KeyCode.Keypad6: return Key.Numpad6;
                case KeyCode.Keypad7: return Key.Numpad7;
                case KeyCode.Keypad8: return Key.Numpad8;
                case KeyCode.Keypad9: return Key.Numpad9;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                default: return Key.None;
            }
        }
#endif

        private void OnBuffApplied(BuffBase buff)
        {
            if (buff == null || buff.Instance == null)
            {
                return;
            }

            Debug.Log(
                $"[Event] Buff applied: id={buff.Instance.BuffId}, stack={buff.Instance.stack}, target={target.name}",
                this);
        }

        private void OnBuffRemoved(BuffBase buff)
        {
            if (buff == null || buff.Instance == null)
            {
                return;
            }

            Debug.Log(
                $"[Event] Buff removed: id={buff.Instance.BuffId}, target={target.name}",
                this);
        }
    }
}
