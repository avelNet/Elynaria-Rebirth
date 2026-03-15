using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3.0f;
        [SerializeField] private float _runningSpeed = 4.5f;
        [SerializeField] private float _sprintSpeed = 6.0f;
        [SerializeField] private float _currentSpeed;
        private Vector3 _moveDirection;
        private float _rotationSmoothTime = 0.12f;
        private float _speedChangeRate = 20.0f;

        [Header("Bool")]
        private bool _isRunning;
        private bool _isRunMode;
        private bool _isWalking;
        private bool _isSprinting;
        private bool _isJumping;
        private bool _wasRunningAtJumpStart;
        private bool _wasWalkingAtJumpStart;
        private bool _wasSprintingAtJumpStart;

        [Header("Gravity AND Jump")]
        [SerializeField] private float _gravity = -15.0f;
        private float _verticalVelocity;
        private float _groundedOffset = -0.14f;
        private float _groundRadius = 0.20f;
        [SerializeField] private LayerMask GroundLayers;
        private float _jumpTimeoutDelta;
        private float JumpHeight = 1.2f;
        private float JumpTimeout = 0.1f;

        [Header("Components")]
        [SerializeField] private GameObject CinemachineCameraTarget;
        private CharacterController _controller;
        private PlayerInputSystem _inputActions;
        private Transform _mainCamera;

        [Header("Camera")]
        [SerializeField] private float TopClamp = 70.0f;
        [SerializeField] private float BottomClamp = -30.0f;
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;

        [Header("Sprint Jump Cooldown")]
        [SerializeField] private float _sprintJumpCooldown = 0.3f; // Длительность КД
        private float _sprintJumpCooldownDelta; // Счетчик времени

        private float _ctrlInput =>
            _inputActions.Player.WalkToggle.ReadValue<float>();
        private float _shiftInput =>
            _inputActions.Player.Sprint.ReadValue<float>();

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _mainCamera = Camera.main.transform;

            _inputActions = new PlayerInputSystem();
        }

        private void OnEnable()
        {
            _inputActions.Player.WalkToggle.performed += WalkToggle_performed;
            _inputActions.Player.Jump.performed += Jump_performed;
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.WalkToggle.performed -= WalkToggle_performed;
            _inputActions.Player.Jump.performed -= Jump_performed;
            _inputActions.Disable();
        }

        private void WalkToggle_performed(InputAction.CallbackContext context)
        {
            _isRunMode = !_isRunMode;
        }
        private void Jump_performed(InputAction.CallbackContext context)
        {
            _isJumping = true;
        }

        private void Update()
        {
            Move();
            Jump();
        }

        private void Move()
        {
            Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();
            bool isGrounded = IsGrounded();
            bool hasInput = input.sqrMagnitude > 0.01f; // Проверка на наличие ввода

            if (hasInput)
            {
                float targetSpeed;
                if (_isRunMode)
                {
                    _isWalking = false;
                    if (_shiftInput > 0.5f)
                    {
                        targetSpeed = _sprintSpeed;
                        _isSprinting = true; // Сразу ставим флаг спринта
                        _isRunning = true;
                    }
                    else
                    {
                        targetSpeed = _runningSpeed;
                        _isSprinting = false;
                        _isRunning = true; // Даже если скорость еще не набрана, мы УЖЕ бежим
                    }
                }
                else
                {
                    targetSpeed = _moveSpeed;
                    _isWalking = true;
                    _isRunning = false;
                    _isSprinting = false;
                }

                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * _speedChangeRate);

                _targetRotation = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _mainCamera.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                _moveDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            }
            else
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, _speedChangeRate * Time.deltaTime);
                _isRunning = false;
                _isWalking = false;
                _isSprinting = false;

                if (isGrounded && _currentSpeed < 0.05f)
                {
                    _moveDirection = Vector3.zero;
                }
            }

            Vector3 move = _moveDirection * (_currentSpeed * Time.deltaTime);
            move.y = _verticalVelocity * Time.deltaTime;
            _controller.Move(move);
        }

        private void Jump()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
            bool isPhysicsGrounded = Physics.CheckSphere(spherePosition, _groundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (isPhysicsGrounded)
            {
                // Таймер КД уменьшается, пока мы на земле
                if (_sprintJumpCooldownDelta > 0.0f)
                {
                    _sprintJumpCooldownDelta -= Time.deltaTime;
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                    _wasRunningAtJumpStart = false;
                    _wasWalkingAtJumpStart = false;
                    _wasSprintingAtJumpStart = false;
                }

                // ПРОВЕРКА: Можно ли прыгать?
                // КД теперь срабатывает, если мы либо бежим (_isRunning), либо ускоряемся (_isSprinting)
                bool isMovingFast = _isRunning || _isSprinting;
                bool canJumpByCooldown = !isMovingFast || _sprintJumpCooldownDelta <= 0.0f;

                if (_isJumping && _jumpTimeoutDelta <= 0.0f && canJumpByCooldown)
                {
                    _wasSprintingAtJumpStart = _isSprinting;
                    _wasRunningAtJumpStart = _isRunning;
                    _wasWalkingAtJumpStart = _isWalking && !_isRunning;

                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * _gravity);
                    _isJumping = false;

                    // Если прыжок совершен во время движения — запускаем КД
                    if (isMovingFast)
                    {
                        _sprintJumpCooldownDelta = _sprintJumpCooldown;
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _isJumping = false;
                _jumpTimeoutDelta = JumpTimeout;
                _verticalVelocity += _gravity * Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
            Vector2 _look = _inputActions.Player.Look.ReadValue<Vector2>();
            if (_look.sqrMagnitude >= 0.01f)
            {
                _cinemachineTargetYaw += _look.x;
                _cinemachineTargetPitch += _look.y;
            }
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
        public bool IsGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
            return Physics.CheckSphere(spherePosition, _groundRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        public bool IsWalking()
        {
            return _isWalking;
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public bool IsSprinting()
        {
            return _isSprinting;
        }

        public bool IsJumping()
        {
            return _isJumping;
        }

        public bool WasRunningAtJumpStart()
        {
            return _wasRunningAtJumpStart;
        }

        public bool WasWalkingAtJumpStart()
        {
            return _wasWalkingAtJumpStart;
        }

        public bool WasSprintingAtJumpStart()
        {
            return _wasSprintingAtJumpStart;
        }
    }
}