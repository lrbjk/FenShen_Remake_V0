using UnityEngine;
using UnityEngine.Events;

namespace FenShen.PlayerFSM
{
    public class AbilityGate : MonoBehaviour
    {
        [Header("Requirement")]
        public AbilityId requiredAbility = AbilityId.None;
        [Min(1)]
        public int requiredLevel = 1;

        [Header("Trigger")]
        public bool autoCheckOnTriggerEnter = true;
        public string playerTag = "Player";

        [Header("Response")]
        public bool disableBarrierWhenUnlocked = true;
        public Collider barrierCollider;
        public GameObject[] objectsToDisable = new GameObject[0];
        public UnityEvent onGatePassed;
        public UnityEvent onGateBlocked;

        public bool HasAccess(PlayerFsm fsm)
        {
            return fsm != null && fsm.HasAbility(requiredAbility, requiredLevel);
        }

        public bool TryResolve(PlayerFsm fsm)
        {
            bool granted = HasAccess(fsm);
            if (granted)
            {
                if (disableBarrierWhenUnlocked)
                {
                    if (barrierCollider != null)
                    {
                        barrierCollider.enabled = false;
                    }

                    for (int i = 0; i < objectsToDisable.Length; i++)
                    {
                        if (objectsToDisable[i] != null)
                        {
                            objectsToDisable[i].SetActive(false);
                        }
                    }
                }

                onGatePassed?.Invoke();
                return true;
            }

            onGateBlocked?.Invoke();
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoCheckOnTriggerEnter || other == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag) && !other.CompareTag(playerTag))
            {
                return;
            }

            PlayerFsm fsm = other.GetComponentInParent<PlayerFsm>();
            if (fsm == null)
            {
                return;
            }

            TryResolve(fsm);
        }
    }
}
