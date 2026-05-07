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

    public class CombatPrototypeRuntime : MonoBehaviour
    {
        public static CombatPrototypeRuntime Instance { get; private set; }

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

        [Header("Animation")]
        public string idleState = "Idle";
        public string runState = "Move";
        public string jumpState = "JumpStart";
        public string fallState = "Fall";
        public string dodgeState = "GroundDash";
        public string launchAttackState = "AttackUp";
        public string[] groundAttackStates = new string[] { "Attack01", "Attack02", "Attack03", "Attack04" };
        public string[] airAttackStates = new string[] { "AirAttack01", "AirAttack02", "AirAttack03" };

        private Rigidbody2D _body;
        private SpriteRenderer _renderer;
        private Animator _animator;
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
        private bool _attackRecoveryCancelable;
        private bool _queuedAttackCancel;
        private Coroutine _attackRoutine;
        private CombatPrototypeChaserClone _activeChaser;
        private CombatPrototypeMimicClone _activeMimic;
        private string _currentAnimationState;

        private bool _prevJump;
        private bool _prevDash;
        private bool _prevAttack;
        private bool _prevSkill;
        private bool _prevRt;
        private bool _prevRb;
        private bool _loggedRuntimeReady;

        void Awake()
        {
            Instance = this;
            _body = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();
            _damageable = GetComponent<PrototypeDamageable>();
            _body.gravityScale = gravityScale;
            _body.freezeRotation = true;
            Debug.Log("[CombatPrototype] Player controller awake. Rigidbody=" + (_body != null) + " Animator=" + (_animator != null));
        }

        void Update()
        {
            ReadInput();
            UpdateGrounded();
            UpdateFacing();
            HandleActions();
            UpdateBaseAnimation();
            UpdateVisuals();
            StoreInputEdges();
            if (!_loggedRuntimeReady)
            {
                _loggedRuntimeReady = true;
                Debug.Log("[CombatPrototype] Runtime input ready. Gamepad=" + (Gamepad.current != null) + " Joystick=" + (Joystick.current != null) + " Keyboard=" + (Keyboard.current != null));
            }
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
            _isAttacking = false;
            _attackRecoveryCancelable = false;
            _queuedAttackCancel = false;
            Pulse(new Color(0.2f, 1f, 1f));
            return true;
        }

        private void ReadInput()
        {
            Gamepad pad = Gamepad.current;
            Joystick joystick = Joystick.current;
            Keyboard keyboard = Keyboard.current;

            Vector2 padMove = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
            Vector2 joystickMove = joystick != null ? joystick.stick.ReadValue() : Vector2.zero;
            Vector2 keyMove = Vector2.zero;
            if (keyboard != null)
            {
                keyMove.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                keyMove.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            _moveInput = padMove.sqrMagnitude > 0.04f ? padMove : (joystickMove.sqrMagnitude > 0.04f ? joystickMove : keyMove);
            if (_moveInput.sqrMagnitude <= 0.04f)
            {
                _moveInput = ReadLegacyMove();
            }

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
            PlayAnimation(jumpState, true);
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
            PlayAnimation(dodgeState, true);
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
            if (_isAttacking)
            {
                if (_attackRecoveryCancelable)
                {
                    _queuedAttackCancel = true;
                }

                return;
            }

            if (_moveInput.y > 0.45f && _isGrounded)
            {
                StartAttack(launchAttackState, 0.12f, 0.05f, 0.18f, groundAttackDamage + 2f, groundAttackBox, new Vector2(0.75f, 0.2f), new Vector2(_facing * 2.5f, 2.5f), true, false);
                return;
            }

            if (_isGrounded)
            {
                if (Time.time - _lastAttackAt > comboResetTime)
                {
                    _groundComboIndex = 0;
                }

                _groundComboIndex = (_groundComboIndex % 4) + 1;
                float damage = groundAttackDamage + _groundComboIndex * 1.75f;
                bool finisher = _groundComboIndex == 4;
                Vector2 knockback = new Vector2(_facing * (2f + _groundComboIndex), finisher ? 2.5f : 0.8f);
                string stateName = GetAnimationState(groundAttackStates, _groundComboIndex - 1, "Attack");
                StartAttack(stateName, 0.08f, 0.04f, 0.16f + _groundComboIndex * 0.03f, damage, groundAttackBox, new Vector2(0.72f, 0.12f), knockback, false, false);
            }
            else if (!_airAttackLocked)
            {
                _airComboIndex = (_airComboIndex % 3) + 1;
                bool slam = _airComboIndex == 3;
                Vector2 knockback = slam ? new Vector2(_facing * 2f, -8f) : new Vector2(_facing * 2f, 2f);
                string stateName = GetAnimationState(airAttackStates, _airComboIndex - 1, "AirAttack");
                StartAttack(stateName, 0.07f, 0.04f, 0.14f, airAttackDamage + _airComboIndex, airAttackBox, new Vector2(0.65f, 0f), knockback, false, slam);
                if (slam)
                {
                    _airAttackLocked = true;
                    _body.linearVelocity = new Vector2(_body.linearVelocity.x, -6f);
                }
            }
        }

        private void StartAttack(string animationState, float startup, float active, float recovery, float damage, Vector2 box, Vector2 offset, Vector2 knockback, bool launch, bool slam)
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
            }

            _attackRoutine = StartCoroutine(AttackRoutine(animationState, startup, active, recovery, damage, box, offset, knockback, launch, slam));
        }

        private IEnumerator AttackRoutine(string animationState, float startup, float active, float recovery, float damage, Vector2 box, Vector2 offset, Vector2 knockback, bool launch, bool slam)
        {
            _isAttacking = true;
            _attackRecoveryCancelable = false;
            _queuedAttackCancel = false;
            _lastAttackAt = Time.time;
            PlayAnimation(animationState, true);

            yield return new WaitForSeconds(startup);

            if (_activeMimic != null)
            {
                _activeMimic.MimicAttack(box, offset, damage * 0.65f, knockback, launch, slam);
            }

            DealDamage(box, offset, ConsumeEmpowered(damage), knockback, launch, slam);
            yield return new WaitForSeconds(active);

            _attackRecoveryCancelable = true;
            float recoveryEndAt = Time.time + recovery;
            while (Time.time < recoveryEndAt)
            {
                if (_queuedAttackCancel)
                {
                    _isAttacking = false;
                    _attackRecoveryCancelable = false;
                    _queuedAttackCancel = false;
                    _attackRoutine = null;
                    TryAttack();
                    yield break;
                }

                yield return null;
            }

            _attackRecoveryCancelable = false;
            _isAttacking = false;
            _attackRoutine = null;
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
            PlayAnimation("AirAttack04", true);
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
            PlayAnimation(lift ? "SpAttack" : launchAttackState, true);
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

        private void UpdateBaseAnimation()
        {
            if (_isAttacking || _isDashing)
            {
                return;
            }

            if (!_isGrounded)
            {
                PlayAnimation(_body.linearVelocity.y > 0.1f ? jumpState : fallState, false);
                return;
            }

            PlayAnimation(Mathf.Abs(_moveInput.x) > 0.15f ? runState : idleState, false);
        }

        private void PlayAnimation(string stateName, bool restart)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            if (!restart && _currentAnimationState == stateName)
            {
                return;
            }

            _currentAnimationState = stateName;
            _animator.Play(stateName, 0, restart ? 0f : float.NegativeInfinity);
        }

        private string GetAnimationState(string[] states, int index, string fallback)
        {
            if (states != null && index >= 0 && index < states.Length && !string.IsNullOrEmpty(states[index]))
            {
                return states[index];
            }

            return fallback;
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

        private bool CurrentJump() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonSouth : null, Keyboard.current != null ? Keyboard.current.spaceKey : null, 0); }
        private bool CurrentDash() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonEast : null, Keyboard.current != null ? Keyboard.current.leftShiftKey : null, 1); }
        private bool CurrentAttack() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonWest : null, Keyboard.current != null ? Keyboard.current.jKey : null, 2); }
        private bool CurrentSkill() { return ReadButton(Gamepad.current != null ? Gamepad.current.buttonNorth : null, Keyboard.current != null ? Keyboard.current.kKey : null, 3); }
        private bool CurrentRb() { return ReadButton(Gamepad.current != null ? Gamepad.current.rightShoulder : null, Keyboard.current != null ? Keyboard.current.oKey : null, 5); }

        private bool CurrentRt()
        {
            bool pad = Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.35f;
            bool key = Keyboard.current != null && Keyboard.current.iKey.isPressed;
            return pad || key || ReadJoystickButton(6) || ReadJoystickButton(7) || ReadLegacyButton("Fire3");
        }

        private bool ReadButton(ButtonControl padButton, KeyControl key, int joystickButtonIndex)
        {
            return (padButton != null && padButton.isPressed) || (key != null && key.isPressed) || ReadJoystickButton(joystickButtonIndex) || ReadLegacyButtonForKey(key);
        }

        private bool ReadJoystickButton(int buttonIndex)
        {
            Joystick joystick = Joystick.current;
            if (joystick == null || buttonIndex < 0)
            {
                return false;
            }

            int seenButtons = 0;
            foreach (InputControl control in joystick.allControls)
            {
                ButtonControl button = control as ButtonControl;
                if (button == null)
                {
                    continue;
                }

                if (seenButtons == buttonIndex)
                {
                    return button.isPressed;
                }

                seenButtons++;
            }

            return false;
        }

        private Vector2 ReadLegacyMove()
        {
            try
            {
                return new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
            }
            catch (System.InvalidOperationException)
            {
                return Vector2.zero;
            }
        }

        private bool ReadLegacyButtonForKey(KeyControl key)
        {
            if (key == null)
            {
                return false;
            }

            if (key == Keyboard.current.spaceKey)
            {
                return ReadLegacyButton("Jump");
            }

            if (key == Keyboard.current.leftShiftKey)
            {
                return ReadLegacyKey(KeyCode.LeftShift) || ReadLegacyButton("Fire2");
            }

            if (key == Keyboard.current.jKey)
            {
                return ReadLegacyKey(KeyCode.J) || ReadLegacyButton("Fire1") || ReadLegacyJoystickButton(2);
            }

            if (key == Keyboard.current.kKey)
            {
                return ReadLegacyKey(KeyCode.K) || ReadLegacyJoystickButton(3);
            }

            if (key == Keyboard.current.oKey)
            {
                return ReadLegacyKey(KeyCode.O) || ReadLegacyJoystickButton(5);
            }

            return false;
        }

        private bool ReadLegacyButton(string buttonName)
        {
            try
            {
                return UnityEngine.Input.GetButton(buttonName);
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
            catch (System.ArgumentException)
            {
                return false;
            }
        }

        private bool ReadLegacyKey(KeyCode key)
        {
            try
            {
                return UnityEngine.Input.GetKey(key);
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
        }

        private bool ReadLegacyJoystickButton(int button)
        {
            try
            {
                return UnityEngine.Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + button));
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
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
                "Empowered: perfect dodge during enemy strike window boosts next attack.\n" +
                "Debug Move=" + _moveInput + " Gamepad=" + (Gamepad.current != null) + " Joystick=" + (Joystick.current != null) + " Keyboard=" + (Keyboard.current != null));
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
