using System;
using System.Collections.Generic;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    public class PlayerAbilityController : MonoBehaviour
    {
        [Serializable]
        public class AbilityState
        {
            public AbilityId id = AbilityId.None;
            public bool unlocked;
            [Min(1)]
            public int level = 1;

            public AbilityState Clone()
            {
                return new AbilityState
                {
                    id = id,
                    unlocked = unlocked,
                    level = Mathf.Max(1, level)
                };
            }
        }

        [Header("Config")]
        public AbilityLoadoutSO defaultLoadout;

        [Header("Runtime")]
        [SerializeField]
        private List<AbilityState> runtimeStates = new List<AbilityState>();

        private readonly Dictionary<AbilityId, AbilityState> _stateMap = new Dictionary<AbilityId, AbilityState>();
        private bool _initialized;

        public event Action<AbilityId, bool, int> AbilityChanged;

        void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            RebuildRuntimeState();
            _initialized = true;
        }

        public bool HasAbility(AbilityId id, int minLevel = 1)
        {
            EnsureInitialized();
            if (id == AbilityId.None)
            {
                return true;
            }

            if (!_stateMap.TryGetValue(id, out AbilityState state))
            {
                return false;
            }

            return state.unlocked && state.level >= Mathf.Max(1, minLevel);
        }

        public int GetAbilityLevel(AbilityId id)
        {
            EnsureInitialized();
            if (_stateMap.TryGetValue(id, out AbilityState state) && state.unlocked)
            {
                return state.level;
            }

            return 0;
        }

        public bool UnlockAbility(AbilityId id, int level = 1)
        {
            if (id == AbilityId.None)
            {
                return false;
            }

            EnsureInitialized();
            AbilityState state = GetOrCreateState(id);
            int resolvedLevel = Mathf.Max(1, level);
            bool changed = !state.unlocked || state.level != resolvedLevel;
            state.unlocked = true;
            state.level = resolvedLevel;
            NotifyAbilityChanged(state, changed);
            return changed;
        }

        public bool LockAbility(AbilityId id)
        {
            if (id == AbilityId.None)
            {
                return false;
            }

            EnsureInitialized();
            AbilityState state = GetOrCreateState(id);
            bool changed = state.unlocked;
            state.unlocked = false;
            NotifyAbilityChanged(state, changed);
            return changed;
        }

        public bool TryHandleAbilityInput(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            PlayerCloneAbilityController cloneController = GetComponent<PlayerCloneAbilityController>();
            return cloneController != null && cloneController.TryHandleInput(fsm, this);
        }

        public void ResetToDefaultLoadout()
        {
            _initialized = false;
            runtimeStates.Clear();
            RebuildRuntimeState();
            _initialized = true;
        }

        private void RebuildRuntimeState()
        {
            _stateMap.Clear();

            if (runtimeStates == null)
            {
                runtimeStates = new List<AbilityState>();
            }

            bool hasRuntimeOverrides = runtimeStates.Count > 0;
            if (!hasRuntimeOverrides && defaultLoadout != null)
            {
                for (int i = 0; i < defaultLoadout.abilities.Count; i++)
                {
                    var definition = defaultLoadout.abilities[i];
                    if (definition == null || definition.id == AbilityId.None)
                    {
                        continue;
                    }

                    runtimeStates.Add(new AbilityState
                    {
                        id = definition.id,
                        unlocked = definition.unlockedByDefault,
                        level = Mathf.Max(1, definition.defaultLevel)
                    });
                }
            }

            for (int i = 0; i < runtimeStates.Count; i++)
            {
                AbilityState state = runtimeStates[i];
                if (state == null || state.id == AbilityId.None)
                {
                    continue;
                }

                state.level = Mathf.Max(1, state.level);
                _stateMap[state.id] = state;
            }
        }

        private AbilityState GetOrCreateState(AbilityId id)
        {
            if (_stateMap.TryGetValue(id, out AbilityState state))
            {
                return state;
            }

            state = new AbilityState
            {
                id = id,
                unlocked = false,
                level = 1
            };
            runtimeStates.Add(state);
            _stateMap[id] = state;
            return state;
        }

        private void NotifyAbilityChanged(AbilityState state, bool changed)
        {
            if (!changed || state == null)
            {
                return;
            }

            AbilityChanged?.Invoke(state.id, state.unlocked, state.level);
        }

        [ContextMenu("Reset Runtime State To Default Loadout")]
        private void ResetRuntimeStateToDefaultLoadoutContextMenu()
        {
            runtimeStates = new List<AbilityState>();
            ResetToDefaultLoadout();
        }
    }
}
