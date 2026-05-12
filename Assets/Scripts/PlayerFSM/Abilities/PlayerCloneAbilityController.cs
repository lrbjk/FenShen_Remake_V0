using FenShen.Combat;
using FenShen.GameData;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [RequireComponent(typeof(PlayerAbilityController))]
    public class PlayerCloneAbilityController : MonoBehaviour
    {
        [Header("分身通用")]
        [InspectorName("分身预制体")]
        [SerializeField] private GameObject clonePrefab;
        [InspectorName("敌人检测层")]
        [SerializeField] private LayerMask enemyLayers = ~0;
        [InspectorName("检测半径")]
        [SerializeField, Min(0f)] private float targetSearchRadius = 8f;
        [InspectorName("打印调试日志")]
        [SerializeField] private bool logDebug;
        [InspectorName("生成前方距离")]
        [SerializeField, Min(0f)] private float forwardSpawnDistance = 1.2f;
        [InspectorName("生成身后距离")]
        [SerializeField, Min(0f)] private float behindTargetDistance = 1f;

        [Header("RT+RB 诱饵冲刺")]
        [InspectorName("诱饵持续时间")]
        [SerializeField, Min(0f)] private float decoyLifetime = 2f;
        [InspectorName("诱饵冷却")]
        [SerializeField, Min(0f)] private float decoyCooldown = 0.4f;
        [InspectorName("玩家冲刺距离")]
        [SerializeField, Min(0f)] private float decoyDashDistance = 3f;

        [Header("RT+X 攻击换位")]
        [InspectorName("攻击分身持续时间")]
        [SerializeField, Min(0f)] private float assaultLifetime = 4f;
        [InspectorName("攻击分身冷却")]
        [SerializeField, Min(0f)] private float assaultCooldown = 0.8f;
        [InspectorName("攻击分身移动速度")]
        [SerializeField, Min(0f)] private float assaultMoveSpeed = 8f;
        [InspectorName("攻击分身伤害")]
        [SerializeField, Min(0f)] private float assaultDamage = 12f;
        [InspectorName("攻击分身命中半径")]
        [SerializeField, Min(0f)] private float assaultHitRadius = 0.8f;

        [Header("RT+Y 模仿攻击")]
        [InspectorName("模仿持续时间")]
        [SerializeField, Min(0f)] private float mimicLifetime = 2f;
        [InspectorName("模仿冷却")]
        [SerializeField, Min(0f)] private float mimicCooldown = 1.2f;
        [InspectorName("模仿伤害倍率")]
        [SerializeField, Min(0f)] private float mimicDamageMultiplier = 0.75f;
        [InspectorName("模仿命中半径")]
        [SerializeField, Min(0f)] private float mimicHitRadius = 1f;

        private PlayerFsm _fsm;
        private PlayerCloneRuntime _decoyClone;
        private PlayerCloneRuntime _assaultClone;
        private PlayerCloneRuntime _mimicClone;
        private float _decoyReadyTime;
        private float _assaultReadyTime;
        private float _mimicReadyTime;

        void Awake()
        {
            _fsm = GetComponent<PlayerFsm>();
        }

        void OnDisable()
        {
            ReleaseClone(ref _decoyClone);
            ReleaseClone(ref _assaultClone);
            ReleaseClone(ref _mimicClone);
        }

        public bool TryHandleInput(PlayerFsm fsm, PlayerAbilityController abilityController)
        {
            if (fsm == null || abilityController == null)
            {
                return false;
            }

            _fsm = fsm;
            if (fsm.CoreModifierPressedThisFrame())
            {
                if (TrySwapWithAssaultClone())
                {
                    return true;
                }

                return TrySwapWithDecoyClone();
            }

            if (!fsm.CoreModifierIsHeld())
            {
                return false;
            }

            if (fsm.RightShoulderPressedThisFrame() && abilityController.HasAbility(AbilityId.CloneDecoyDash))
            {
                if (!IsReady(_decoyReadyTime, "Decoy"))
                {
                    return true;
                }

                SpawnDecoyAndDash();
                _decoyReadyTime = Time.time + decoyCooldown;
                return true;
            }

            if (fsm.FaceXPressedThisFrame() && abilityController.HasAbility(AbilityId.CloneAssaultSwap))
            {
                if (!IsReady(_assaultReadyTime, "Assault"))
                {
                    return true;
                }

                SpawnAssaultClone();
                _assaultReadyTime = Time.time + assaultCooldown;
                return true;
            }

            if (fsm.FaceYPressedThisFrame() && abilityController.HasAbility(AbilityId.CloneMimic))
            {
                if (!IsReady(_mimicReadyTime, "Mimic"))
                {
                    return true;
                }

                SpawnMimicClone();
                _mimicReadyTime = Time.time + mimicCooldown;
                return true;
            }

            return false;
        }

        private void SpawnDecoyAndDash()
        {
            Vector3 facing = ResolveFacing();
            Vector3 spawnPosition = ResolvePlayerPosition();
            ReleaseClone(ref _decoyClone);
            _decoyClone = SpawnClone(spawnPosition, Quaternion.identity, decoyLifetime);
            if (_decoyClone != null)
            {
                _decoyClone.InitializeDecoy(decoyLifetime);
                Log("Spawned decoy clone.");
            }

            TranslatePlayer(facing * decoyDashDistance);
        }

        private void SpawnAssaultClone()
        {
            Transform target = FindNearestEnemy();
            if (target == null)
            {
                Log("Assault clone failed: no enemy target.");
                return;
            }

            ReleaseClone(ref _assaultClone);
            Vector3 spawnPosition = ResolvePlayerPosition() + ResolveFacing() * forwardSpawnDistance;
            _assaultClone = SpawnClone(spawnPosition, Quaternion.identity, assaultLifetime);
            if (_assaultClone != null)
            {
                _assaultClone.InitializeAssault(target, assaultMoveSpeed, assaultDamage, assaultHitRadius, enemyLayers, assaultLifetime);
                Log("Spawned assault clone.");
            }
        }

        private void SpawnMimicClone()
        {
            Transform target = FindNearestEnemy();
            if (target == null || _fsm == null)
            {
                Log("Mimic clone failed: no enemy target.");
                return;
            }

            ReleaseClone(ref _mimicClone);
            Vector3 directionFromTarget = (ResolvePlayerPosition() - target.position).normalized;
            if (directionFromTarget.sqrMagnitude < 0.0001f)
            {
                directionFromTarget = -ResolveFacing();
            }

            Vector3 spawnPosition = target.position + directionFromTarget * behindTargetDistance;
            _mimicClone = SpawnClone(spawnPosition, Quaternion.identity, mimicLifetime);
            if (_mimicClone != null)
            {
                float ownerAttack = _fsm.GetStat(StatKeys.Attack, 10f);
                _mimicClone.InitializeMimic(_fsm.CombatController, ownerAttack, mimicDamageMultiplier, mimicHitRadius, enemyLayers, mimicLifetime);
                Log("Spawned mimic clone.");
            }
        }

        private bool TrySwapWithAssaultClone()
        {
            if (_assaultClone == null || !_assaultClone.IsAvailableForSwap || _fsm == null || _fsm.Character == null)
            {
                Log("Swap failed: assault clone is not available.");
                return false;
            }

            Vector3 playerPosition = _fsm.Character.position;
            _fsm.Character.position = _assaultClone.transform.position;
            _assaultClone.transform.position = playerPosition;
            ReleaseClone(ref _assaultClone);
            Log("Swapped with assault clone.");
            return true;
        }

        private bool TrySwapWithDecoyClone()
        {
            if (_decoyClone == null || !_decoyClone.IsAvailableForSwap || _fsm == null || _fsm.Character == null)
            {
                Log("Swap failed: decoy clone is not available.");
                return false;
            }

            Vector3 playerPosition = _fsm.Character.position;
            _fsm.Character.position = _decoyClone.transform.position;
            _decoyClone.transform.position = playerPosition;
            Log("Swapped with decoy clone.");
            return true;
        }

        private PlayerCloneRuntime SpawnClone(Vector3 position, Quaternion rotation, float lifetime)
        {
            GameObject instance;
            if (clonePrefab != null)
            {
                PooledObject pooledObject = PooledObjectSpawner.Spawn(clonePrefab, position, rotation, lifetime);
                instance = pooledObject != null ? pooledObject.gameObject : Instantiate(clonePrefab, position, rotation);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                instance.name = "Runtime Clone";
                instance.transform.SetPositionAndRotation(position, rotation);
            }

            PlayerCloneRuntime clone = instance.GetComponent<PlayerCloneRuntime>();
            if (clone == null)
            {
                clone = instance.AddComponent<PlayerCloneRuntime>();
            }

            return clone;
        }

        private Transform FindNearestEnemy()
        {
            Vector3 origin = ResolvePlayerPosition();
            Collider[] hits = Physics.OverlapSphere(origin, targetSearchRadius, enemyLayers);
            Transform best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                CombatHurtbox hurtbox = hit.GetComponentInParent<CombatHurtbox>();
                if (hurtbox == null || hurtbox.Team == CombatTeam.Player)
                {
                    continue;
                }

                float distance = (hurtbox.RootTransform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = hurtbox.RootTransform;
                }
            }

            return best;
        }

        private Vector3 ResolvePlayerPosition()
        {
            if (_fsm != null && _fsm.Character != null)
            {
                return _fsm.Character.position;
            }

            return transform.position;
        }

        private Vector3 ResolveFacing()
        {
            if (_fsm != null && _fsm.Character != null && _fsm.Character.localScale.x < 0f)
            {
                return Vector3.left;
            }

            return Vector3.right;
        }

        private void TranslatePlayer(Vector3 delta)
        {
            if (_fsm != null && _fsm.Character != null)
            {
                _fsm.Character.Translate(delta, Space.World);
            }
        }

        private void ReleaseClone(ref PlayerCloneRuntime clone)
        {
            if (clone == null)
            {
                return;
            }

            clone.Release();
            clone = null;
        }

        private bool IsReady(float readyTime, string label)
        {
            bool ready = Time.time >= readyTime;
            if (!ready)
            {
                Log(label + " clone is on cooldown.");
            }

            return ready;
        }

        private void Log(string message)
        {
            if (!logDebug)
            {
                return;
            }

            Debug.Log("[CloneAbility] " + message, this);
        }
    }
}
