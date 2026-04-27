using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FenShen.CombatPrototype
{
    public enum PrototypeTeam
    {
        Player,
        Enemy,
        Decoy
    }

    public class PrototypeDamageable : MonoBehaviour
    {
        public PrototypeTeam team = PrototypeTeam.Enemy;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public bool destroyOnDeath = true;
        public float hitFlashTime = 0.08f;

        private SpriteRenderer _renderer;
        private Rigidbody2D _body;
        private Color _baseColor;
        private Coroutine _flashRoutine;

        public bool IsAlive { get { return currentHealth > 0f; } }

        void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _body = GetComponent<Rigidbody2D>();
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
            }

            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 1f, maxHealth);
        }

        public void TakeHit(float damage, Vector2 knockback, bool launch, bool slam)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
                _body.AddForce(knockback, ForceMode2D.Impulse);
                if (launch)
                {
                    _body.AddForce(Vector2.up * 8f, ForceMode2D.Impulse);
                }
                if (slam)
                {
                    _body.AddForce(Vector2.down * 12f, ForceMode2D.Impulse);
                }
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }
            _flashRoutine = StartCoroutine(Flash());

            if (currentHealth <= 0f)
            {
                if (destroyOnDeath)
                {
                    Destroy(gameObject, 0.05f);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator Flash()
        {
            if (_renderer == null)
            {
                yield break;
            }

            _renderer.color = Color.white;
            yield return new WaitForSeconds(hitFlashTime);
            _renderer.color = _baseColor;
        }
    }

    public class CombatPrototypeDecoy : MonoBehaviour
    {
        public float lifetime = 2.5f;

        private float _dieAt;

        public static readonly List<CombatPrototypeDecoy> ActiveDecoys = new List<CombatPrototypeDecoy>();

        void OnEnable()
        {
            _dieAt = Time.time + lifetime;
            ActiveDecoys.Add(this);
        }

        void OnDisable()
        {
            ActiveDecoys.Remove(this);
        }

        void Update()
        {
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
            }
        }
    }

    public class CombatPrototypeEnemy : MonoBehaviour
    {
        public float moveSpeed = 3f;
        public float attackRange = 1.1f;
        public float attackCooldown = 1.2f;
        public float attackDamage = 10f;
        public Vector2 attackBox = new Vector2(1.2f, 1.0f);
        public Vector2 attackOffset = new Vector2(0.7f, 0.2f);

        private Rigidbody2D _body;
        private SpriteRenderer _renderer;
        private PrototypeDamageable _damageable;
        private float _nextAttackAt;
        private float _facing = -1f;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _damageable = GetComponent<PrototypeDamageable>();
        }

        void Update()
        {
            if (_damageable != null && !_damageable.IsAlive)
            {
                return;
            }

            Transform target = FindTarget();
            if (target == null)
            {
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                return;
            }

            float deltaX = target.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.05f)
            {
                _facing = Mathf.Sign(deltaX);
            }

            if (_renderer != null)
            {
                _renderer.flipX = _facing < 0f;
            }

            if (Mathf.Abs(deltaX) > attackRange)
            {
                _body.linearVelocity = new Vector2(_facing * moveSpeed, _body.linearVelocity.y);
            }
            else
            {
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                TryAttack();
            }
        }

        private Transform FindTarget()
        {
            CombatPrototypePlayerController player = CombatPrototypePlayerController.Instance;
            Transform best = player != null ? player.transform : null;
            float bestDistance = best != null ? Vector2.Distance(transform.position, best.position) : float.MaxValue;

            for (int i = CombatPrototypeDecoy.ActiveDecoys.Count - 1; i >= 0; i--)
            {
                CombatPrototypeDecoy decoy = CombatPrototypeDecoy.ActiveDecoys[i];
                if (decoy == null)
                {
                    CombatPrototypeDecoy.ActiveDecoys.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(transform.position, decoy.transform.position);
                if (distance < bestDistance + 3f)
                {
                    best = decoy.transform;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void TryAttack()
        {
            if (Time.time < _nextAttackAt)
            {
                return;
            }

            _nextAttackAt = Time.time + attackCooldown;
            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            if (_renderer != null)
            {
                _renderer.color = new Color(1f, 0.35f, 0.3f);
            }

            yield return new WaitForSeconds(0.18f);

            Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * _facing, attackOffset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBox, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable target = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (target == null || target.team == PrototypeTeam.Enemy)
                {
                    continue;
                }

                CombatPrototypePlayerController player = target.GetComponent<CombatPrototypePlayerController>();
                if (player != null && player.TryPerfectDodgeWindow())
                {
                    continue;
                }

                target.TakeHit(attackDamage, new Vector2(_facing * 4f, 2f), false, false);
            }

            yield return new WaitForSeconds(0.12f);
            if (_renderer != null)
            {
                _renderer.color = new Color(0.95f, 0.24f, 0.26f);
            }
        }
    }

    public class CombatPrototypePlayerController : MonoBehaviour
    {
        public static CombatPrototypePlayerController Instance { get; private set; }

        [Header("Movement")]
        public float moveSpeed = 7f;
        public float jumpForce = 13f;
        public float dashSpeed = 16f;
        public float dashDuration = 0.16f;
        public float dodgeIFrameDuration = 0.2f;
        public float gravityScale = 3.3f;

        [Header("Combat")]
        public float comboResetTime = 0.5f;
        public float groundAttackDamage = 10f;
        public float airAttackDamage = 9f;
        public float empoweredMultiplier = 1.6f;
        public Vector2 groundAttackBox = new Vector2(1.35f, 0.9f);
        public Vector2 airAttackBox = new Vector2(1.2f, 0.9f);

        [Header("Skills")]
        public float skillCooldown = 0.35f;
        public float waveDamage = 16f;
        public float diveDamage = 20f;

        [Header("Clone")]
        public float decoyCooldown = 1.2f;
        public float chaserCooldown = 1.1f;
        public float mimicCooldown = 1.6f;
        public float mimicDuration = 2f;

        private Rigidbody2D _body;
        private SpriteRenderer _renderer;
        private PrototypeDamageable _damageable;
        private Vector2 _moveInput;
        private float _facing = 1f;
        private bool _isGrounded;
        private int _jumpsUsed;
        private bool _airDodgeUsed;
        private bool _airAttackLocked;
        private bool _isDashing;
        private bool _isAttacking;
        private bool _empowered;
        private float _invulnerableUntil;
        private float _lastAttackAt;
        private float _nextSkillAt;
        private float _nextDecoyAt;
        private float _nextChaserAt;
        private float _nextMimicAt;
        private int _groundComboIndex;
        private int _airComboIndex;
        private CombatPrototypeChaserClone _activeChaser;
        private CombatPrototypeMimicClone _activeMimic;

        private bool _prevJump;
        private bool _prevDash;
        private bool _prevAttack;
        private bool _prevSkill;
        private bool _prevRt;
        private bool _prevRb;

        void Awake()
        {
            Instance = this;
            _body = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _damageable = GetComponent<PrototypeDamageable>();
            _body.gravityScale = gravityScale;
            _body.freezeRotation = true;
        }

        void Update()
        {
            ReadInput();
            UpdateGrounded();
            UpdateFacing();
            HandleActions();
            UpdateVisuals();
            StoreInputEdges();
        }

        void FixedUpdate()
        {
            if (_isDashing)
            {
                return;
            }

            float xVelocity = _isAttacking && _isGrounded ? _body.linearVelocity.x * 0.85f : _moveInput.x * moveSpeed;
            _body.linearVelocity = new Vector2(xVelocity, _body.linearVelocity.y);
        }

        public bool TryPerfectDodgeWindow()
        {
            if (Time.time > _invulnerableUntil)
            {
                return false;
            }

            _empowered = true;
            _groundComboIndex = 0;
            _airComboIndex = 0;
            _airAttackLocked = false;
            Pulse(new Color(0.2f, 1f, 1f));
            return true;
        }

        private void ReadInput()
        {
            Gamepad pad = Gamepad.current;
            Keyboard keyboard = Keyboard.current;

            Vector2 padMove = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
            Vector2 keyMove = Vector2.zero;
            if (keyboard != null)
            {
                keyMove.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                keyMove.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            _moveInput = padMove.sqrMagnitude > 0.04f ? padMove : keyMove;
            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }
        }

        private void HandleActions()
        {
            bool jumpPressed = PressedThisFrame(CurrentJump(), _prevJump);
            bool dashPressed = PressedThisFrame(CurrentDash(), _prevDash);
            bool attackPressed = PressedThisFrame(CurrentAttack(), _prevAttack);
            bool skillPressed = PressedThisFrame(CurrentSkill(), _prevSkill);
            bool rtPressed = PressedThisFrame(CurrentRt(), _prevRt);
            bool rtHeld = CurrentRt();
            bool rbPressed = PressedThisFrame(CurrentRb(), _prevRb);

            if (rtHeld && rbPressed && Time.time >= _nextDecoyAt)
            {
                SpawnDecoyDash();
                return;
            }

            if (rtHeld && attackPressed && Time.time >= _nextChaserAt)
            {
                SpawnChaserClone();
                return;
            }

            if (rtPressed && _activeChaser != null)
            {
                SwapWithChaser();
                return;
            }

            if (rtHeld && skillPressed && Time.time >= _nextMimicAt)
            {
                SpawnMimicClone();
                return;
            }

            if (jumpPressed)
            {
                TryJump();
            }

            if (dashPressed)
            {
                TryDash();
            }

            if (skillPressed && !rtHeld && Time.time >= _nextSkillAt)
            {
                TrySkill();
            }

            if (attackPressed && !rtHeld)
            {
                TryAttack();
            }

            if (_isAttacking && !_isGrounded && Time.time - _lastAttackAt > 0.32f && !CurrentAttack())
            {
                _airAttackLocked = true;
            }
        }

        private void TryJump()
        {
            if (_isGrounded)
            {
                _jumpsUsed = 0;
            }

            if (_jumpsUsed >= 2)
            {
                return;
            }

            _jumpsUsed++;
            _airAttackLocked = false;
            _body.linearVelocity = new Vector2(_body.linearVelocity.x, 0f);
            _body.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        private void TryDash()
        {
            if (!_isGrounded && _airDodgeUsed)
            {
                return;
            }

            if (!_isGrounded)
            {
                _airDodgeUsed = true;
                _airAttackLocked = false;
                _airComboIndex = 0;
            }

            _groundComboIndex = 0;
            _invulnerableUntil = Time.time + dodgeIFrameDuration;
            StartCoroutine(DashRoutine(_facing));
        }

        private IEnumerator DashRoutine(float direction)
        {
            _isDashing = true;
            float endAt = Time.time + dashDuration;
            while (Time.time < endAt)
            {
                _body.linearVelocity = new Vector2(direction * dashSpeed, 0f);
                yield return null;
            }
            _isDashing = false;
        }

        private void TryAttack()
        {
            if (_moveInput.y > 0.45f && _isGrounded)
            {
                StartCoroutine(AttackRoutine("Launch", 0.28f, groundAttackDamage + 2f, groundAttackBox, new Vector2(0.75f, 0.2f), new Vector2(_facing * 2.5f, 2.5f), true, false));
                return;
            }

            if (_isGrounded)
            {
                if (Time.time - _lastAttackAt > comboResetTime)
                {
                    _groundComboIndex = 0;
                }

                _groundComboIndex = (_groundComboIndex % 5) + 1;
                float damage = groundAttackDamage + _groundComboIndex * 1.5f;
                Vector2 knockback = new Vector2(_facing * (2f + _groundComboIndex), _groundComboIndex == 5 ? 2.5f : 0.8f);
                StartCoroutine(AttackRoutine("Ground " + _groundComboIndex, 0.18f + _groundComboIndex * 0.025f, damage, groundAttackBox, new Vector2(0.72f, 0.12f), knockback, false, false));
            }
            else if (!_airAttackLocked)
            {
                _airComboIndex = (_airComboIndex % 3) + 1;
                bool slam = _airComboIndex == 3;
                Vector2 knockback = slam ? new Vector2(_facing * 2f, -8f) : new Vector2(_facing * 2f, 2f);
                StartCoroutine(AttackRoutine("Air " + _airComboIndex, 0.2f, airAttackDamage + _airComboIndex, airAttackBox, new Vector2(0.65f, 0f), knockback, false, slam));
                if (slam)
                {
                    _airAttackLocked = true;
                    _body.linearVelocity = new Vector2(_body.linearVelocity.x, -6f);
                }
            }
        }

        private IEnumerator AttackRoutine(string label, float duration, float damage, Vector2 box, Vector2 offset, Vector2 knockback, bool launch, bool slam)
        {
            _isAttacking = true;
            _lastAttackAt = Time.time;
            if (_activeMimic != null)
            {
                _activeMimic.MimicAttack(box, offset, damage * 0.65f, knockback, launch, slam);
            }

            yield return new WaitForSeconds(duration * 0.35f);
            DealDamage(box, offset, ConsumeEmpowered(damage), knockback, launch, slam);
            yield return new WaitForSeconds(duration * 0.65f);
            _isAttacking = false;
        }

        private void TrySkill()
        {
            _nextSkillAt = Time.time + skillCooldown;
            if (!_isGrounded && _moveInput.y < -0.45f)
            {
                StartCoroutine(DiveSkillRoutine());
                return;
            }

            if (_moveInput.y > 0.45f)
            {
                SpawnWave(new Vector2(_facing, 0.65f).normalized, waveDamage, true);
            }
            else
            {
                SpawnWave(new Vector2(_facing, 0f), waveDamage, false);
            }
        }

        private IEnumerator DiveSkillRoutine()
        {
            _isDashing = true;
            float endAt = Time.time + 0.22f;
            while (Time.time < endAt)
            {
                _body.linearVelocity = new Vector2(_facing * 9f, -14f);
                DealDamage(new Vector2(1.2f, 1f), new Vector2(0.55f, -0.3f), diveDamage, new Vector2(_facing * 2f, -10f), false, true);
                yield return null;
            }
            _isDashing = false;
        }

        private void SpawnWave(Vector2 direction, float damage, bool lift)
        {
            GameObject wave = new GameObject(lift ? "Prototype Upward Sword Wave" : "Prototype Sword Wave");
            wave.name = lift ? "Prototype Upward Sword Wave" : "Prototype Sword Wave";
            wave.transform.position = transform.position + new Vector3(_facing * 0.8f, 0.15f, 0f);
            wave.transform.localScale = new Vector3(0.9f, 0.18f, 1f);
            SpriteRenderer sprite = wave.AddComponent<SpriteRenderer>();
            sprite.sprite = CombatPrototypeFactory.WhiteSprite;
            sprite.color = lift ? new Color(0.4f, 0.95f, 1f, 0.85f) : new Color(0.9f, 0.2f, 1f, 0.85f);
            CombatPrototypeProjectile projectile = wave.AddComponent<CombatPrototypeProjectile>();
            projectile.Initialize(PrototypeTeam.Player, direction, 10f, 0.55f, damage, lift);
        }

        private void DealDamage(Vector2 box, Vector2 offset, float damage, Vector2 knockback, bool launch, bool slam)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * _facing, offset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, box, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable target = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (target == null || target.team != PrototypeTeam.Enemy)
                {
                    continue;
                }

                target.TakeHit(damage, knockback, launch, slam);
            }
        }

        private float ConsumeEmpowered(float damage)
        {
            if (!_empowered)
            {
                return damage;
            }

            _empowered = false;
            Pulse(new Color(1f, 0.95f, 0.25f));
            return damage * empoweredMultiplier;
        }

        private void SpawnDecoyDash()
        {
            _nextDecoyAt = Time.time + decoyCooldown;
            GameObject decoy = CombatPrototypeFactory.CreateBody("Decoy Clone", transform.position, new Vector2(0.58f, 1.35f), new Color(0.2f, 1f, 0.85f, 0.55f));
            CombatPrototypeFactory.MakeNonBlockingClone(decoy, gameObject);
            decoy.AddComponent<CombatPrototypeDecoy>();
            PrototypeDamageable damageable = decoy.AddComponent<PrototypeDamageable>();
            damageable.team = PrototypeTeam.Decoy;
            damageable.maxHealth = 40f;
            damageable.currentHealth = 40f;
            StartCoroutine(DashRoutine(_facing));
        }

        private void SpawnChaserClone()
        {
            _nextChaserAt = Time.time + chaserCooldown;
            PrototypeDamageable target = CombatPrototypeFactory.FindNearestEnemy(transform.position);
            Vector3 start = transform.position + Vector3.right * _facing * 0.5f;
            GameObject clone = CombatPrototypeFactory.CreateBody("Chaser Clone", start, new Vector2(0.52f, 1.25f), new Color(0.25f, 0.9f, 1f, 0.78f));
            CombatPrototypeFactory.MakeNonBlockingClone(clone, gameObject);
            _activeChaser = clone.AddComponent<CombatPrototypeChaserClone>();
            _activeChaser.Initialize(this, target);
        }

        private void SwapWithChaser()
        {
            if (_activeChaser == null)
            {
                return;
            }

            Vector3 playerPosition = transform.position;
            transform.position = _activeChaser.transform.position;
            _activeChaser.transform.position = playerPosition;
            Destroy(_activeChaser.gameObject, 0.08f);
            _activeChaser = null;
        }

        private void SpawnMimicClone()
        {
            _nextMimicAt = Time.time + mimicCooldown;
            PrototypeDamageable target = CombatPrototypeFactory.FindNearestEnemy(transform.position);
            Vector3 position = target != null
                ? target.transform.position + Vector3.right * -Mathf.Sign(target.transform.position.x - transform.position.x) * 0.9f
                : transform.position + Vector3.right * _facing * 0.8f;

            GameObject clone = CombatPrototypeFactory.CreateBody("Mimic Clone", position, new Vector2(0.54f, 1.3f), new Color(1f, 0.25f, 0.95f, 0.7f));
            CombatPrototypeFactory.MakeNonBlockingClone(clone, gameObject);
            _activeMimic = clone.AddComponent<CombatPrototypeMimicClone>();
            _activeMimic.Initialize(this, mimicDuration);
        }

        private void UpdateGrounded()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = CombatPrototypeFactory.CheckGround(transform, 0.72f);
            if (_isGrounded && !wasGrounded)
            {
                _jumpsUsed = 0;
                _airDodgeUsed = false;
                _airAttackLocked = false;
                _airComboIndex = 0;
            }
        }

        private void UpdateFacing()
        {
            if (Mathf.Abs(_moveInput.x) > 0.15f)
            {
                _facing = Mathf.Sign(_moveInput.x);
            }
        }

        private void UpdateVisuals()
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.flipX = _facing < 0f;
            if (!_empowered && Time.time > _invulnerableUntil)
            {
                _renderer.color = new Color(0.25f, 0.85f, 1f);
            }
        }

        private void Pulse(Color color)
        {
            if (_renderer != null)
            {
                _renderer.color = color;
            }
        }

        private bool CurrentJump() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonSouth : null, Keyboard.current != null ? Keyboard.current.spaceKey : null); }
        private bool CurrentDash() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonEast : null, Keyboard.current != null ? Keyboard.current.leftShiftKey : null); }
        private bool CurrentAttack() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonWest : null, Keyboard.current != null ? Keyboard.current.jKey : null); }
        private bool CurrentSkill() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonNorth : null, Keyboard.current != null ? Keyboard.current.kKey : null); }
        private bool CurrentRb() { return ReadButton(Gamepad.current != null ? Gamepad.current.rightShoulder : null, Keyboard.current != null ? Keyboard.current.oKey : null); }

        private bool CurrentRt()
        {
            bool pad = Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.35f;
            bool key = Keyboard.current != null && Keyboard.current.iKey.isPressed;
            return pad || key;
        }

        private bool ReadButton(ButtonControl padButton, KeyControl key)
        {
            return (padButton != null && padButton.isPressed) || (key != null && key.isPressed);
        }

        private bool PressedThisFrame(bool current, bool previous)
        {
            return current && !previous;
        }

        private void StoreInputEdges()
        {
            _prevJump = CurrentJump();
            _prevDash = CurrentDash();
            _prevAttack = CurrentAttack();
            _prevSkill = CurrentSkill();
            _prevRt = CurrentRt();
            _prevRb = CurrentRb();
        }

        void OnGUI()
        {
            GUI.Label(new Rect(16f, 16f, 620f, 130f),
                "Combat Prototype\n" +
                "Gamepad: A Jump / B Dash / X Attack / Y Skill / RT+RB Decoy / RT+X Chaser / RT swap / RT+Y Mimic\n" +
                "Keyboard: Space Jump / LeftShift Dash / J Attack / K Skill / I+O Decoy / I+J Chaser / I swap / I+K Mimic\n" +
                "Move: Left Stick or WASD. Up+X launch, Air Down+Y dive, Up+Y anti-air wave.\n" +
                "Empowered: perfect dodge during enemy strike window boosts next attack.");
        }
    }

    public class CombatPrototypeChaserClone : MonoBehaviour
    {
        public float lifetime = 1.8f;
        public float speed = 11f;
        public float attackInterval = 0.2f;

        private CombatPrototypePlayerController _owner;
        private PrototypeDamageable _target;
        private float _dieAt;
        private float _nextAttackAt;

        public void Initialize(CombatPrototypePlayerController owner, PrototypeDamageable target)
        {
            _owner = owner;
            _target = target;
            _dieAt = Time.time + lifetime;
        }

        void Update()
        {
            if (Time.time >= _dieAt || _owner == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_target == null || !_target.IsAlive)
            {
                _target = CombatPrototypeFactory.FindNearestEnemy(transform.position);
            }

            if (_target != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, speed * Time.deltaTime);
                if (Vector2.Distance(transform.position, _target.transform.position) < 0.9f && Time.time >= _nextAttackAt)
                {
                    _nextAttackAt = Time.time + attackInterval;
                    float dir = Mathf.Sign(_target.transform.position.x - transform.position.x);
                    _target.TakeHit(7f, new Vector2(dir * 2.5f, 1.2f), false, false);
                }
            }
            else
            {
                transform.position += Vector3.right * speed * Time.deltaTime;
            }
        }
    }

    public class CombatPrototypeMimicClone : MonoBehaviour
    {
        private CombatPrototypePlayerController _owner;
        private float _dieAt;

        public void Initialize(CombatPrototypePlayerController owner, float lifetime)
        {
            _owner = owner;
            _dieAt = Time.time + lifetime;
        }

        void Update()
        {
            if (Time.time >= _dieAt || _owner == null)
            {
                Destroy(gameObject);
            }
        }

        public void MimicAttack(Vector2 box, Vector2 offset, float damage, Vector2 knockback, bool launch, bool slam)
        {
            StartCoroutine(MimicAttackRoutine(box, offset, damage, knockback, launch, slam));
        }

        private IEnumerator MimicAttackRoutine(Vector2 box, Vector2 offset, float damage, Vector2 knockback, bool launch, bool slam)
        {
            yield return new WaitForSeconds(0.08f);
            PrototypeDamageable target = CombatPrototypeFactory.FindNearestEnemy(transform.position);
            float facing = target != null ? Mathf.Sign(target.transform.position.x - transform.position.x) : 1f;
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * facing, offset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, box, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable damageable = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (damageable == null || damageable.team != PrototypeTeam.Enemy)
                {
                    continue;
                }

                damageable.TakeHit(damage, new Vector2(facing * Mathf.Abs(knockback.x), knockback.y), launch, slam);
            }
        }
    }

    public class CombatPrototypeProjectile : MonoBehaviour
    {
        private PrototypeTeam _team;
        private Vector2 _direction;
        private float _speed;
        private float _damage;
        private float _dieAt;
        private bool _lift;

        public void Initialize(PrototypeTeam team, Vector2 direction, float speed, float lifetime, float damage, bool lift)
        {
            _team = team;
            _direction = direction.normalized;
            _speed = speed;
            _dieAt = Time.time + lifetime;
            _damage = damage;
            _lift = lift;
        }

        void Update()
        {
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
                return;
            }

            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(0.9f, 0.4f), 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PrototypeDamageable target = hits[i].GetComponentInParent<PrototypeDamageable>();
                if (target == null || target.team == _team)
                {
                    continue;
                }

                target.TakeHit(_damage, _lift ? new Vector2(_direction.x * 2f, 5f) : new Vector2(_direction.x * 4f, 1f), _lift, false);
                Destroy(gameObject);
                return;
            }
        }
    }

    public static class CombatPrototypeFactory
    {
        private static Sprite _whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    Texture2D texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                return _whiteSprite;
            }
        }

        public static GameObject CreateBody(string name, Vector3 position, Vector2 size, Color color)
        {
            GameObject body = new GameObject(name);
            body.transform.position = position;
            SpriteRenderer sprite = body.AddComponent<SpriteRenderer>();
            sprite.sprite = WhiteSprite;
            sprite.color = color;
            body.transform.localScale = new Vector3(size.x, size.y, 1f);
            BoxCollider2D collider = body.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            return body;
        }

        public static void MakeNonBlockingClone(GameObject clone, GameObject owner)
        {
            Collider2D cloneCollider = clone.GetComponent<Collider2D>();
            if (cloneCollider != null)
            {
                cloneCollider.isTrigger = true;
            }

            if (owner == null)
            {
                return;
            }

            Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
            Collider2D[] cloneColliders = clone.GetComponentsInChildren<Collider2D>();
            for (int ownerIndex = 0; ownerIndex < ownerColliders.Length; ownerIndex++)
            {
                for (int cloneIndex = 0; cloneIndex < cloneColliders.Length; cloneIndex++)
                {
                    if (ownerColliders[ownerIndex] != null && cloneColliders[cloneIndex] != null)
                    {
                        Physics2D.IgnoreCollision(ownerColliders[ownerIndex], cloneColliders[cloneIndex], true);
                    }
                }
            }
        }

        public static PrototypeDamageable FindNearestEnemy(Vector3 position)
        {
            PrototypeDamageable[] all = Object.FindObjectsByType<PrototypeDamageable>(FindObjectsSortMode.None);
            PrototypeDamageable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].team != PrototypeTeam.Enemy || !all[i].IsAlive)
                {
                    continue;
                }

                float distance = Vector2.Distance(position, all[i].transform.position);
                if (distance < bestDistance)
                {
                    best = all[i];
                    bestDistance = distance;
                }
            }

            return best;
        }

        public static bool CheckGround(Transform transform, float distance)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, distance);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hits[i].collider.GetComponentInParent<PrototypeDamageable>() != null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
