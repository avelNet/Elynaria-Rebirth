using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.0f;
        public float ShiftMultiplier = 1.5f;
        public float SpeedChangeRate = 10.0f;
        public float RotationSmoothTime = 0.12f;

        [Header("Jump & Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.1f;

        [Header("Grounded Check")]
        public float GroundedOffset = -0.14f;
        public float GroundRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;

        // Свойства для Animator
        public float CurrentSpeed => _speed;
        public bool IsWalking => _isWalking;
        public bool IsRunning => _isRunning;
        public bool IsFastRunning => _isFastRunning;
        public bool HasInput => hasInput;

        // Условие остановки: нет ввода + в этом движении хоть раз был спринт + мы на земле
        public bool IsStopping => !hasInput && _hadSprint && _isGrounded;

        private Vector2 _move;
        private Vector2 _look;
        private bool _jump;
        private bool _isWalkingMode = false;
        private bool _isWalking, _isRunning, _isFastRunning;
        private bool _hadSprint;
        private bool _isGrounded;
        private bool hasInput;

        private float _speed;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private CharacterController _controller;
        private PlayerInputSystem _inputActions;
        private GameObject _mainCamera;

        private void Awake()
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _controller = GetComponent<CharacterController>();
            _inputActions = new PlayerInputSystem();
        }

        private void OnEnable()
        {
            _inputActions.Player.Move.performed += ctx => _move = ctx.ReadValue<Vector2>();
            _inputActions.Player.Move.canceled += ctx => _move = Vector2.zero;
            _inputActions.Player.Look.performed += ctx => _look = ctx.ReadValue<Vector2>();
            _inputActions.Player.Look.canceled += ctx => _look = Vector2.zero;
            _inputActions.Player.Jump.performed += ctx => _jump = true;
            _inputActions.Player.WalkToggle.performed += ctx => _isWalkingMode = !_isWalkingMode;
            _inputActions.Enable();
        }

        private void Update()
        {
            Jump();
            Move();
        }

        private void Move()
        {
            hasInput = _move.sqrMagnitude > 0.01f;
            bool isSprintingInput = _inputActions.Player.Sprint.IsPressed();
            float targetSpeed = 0.0f;

            if (hasInput)
            {
                if (_isWalkingMode) targetSpeed = MoveSpeed;
                else targetSpeed = isSprintingInput ? (SprintSpeed * ShiftMultiplier) : SprintSpeed;
            }

            // Ускорение и замедление
            _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * SpeedChangeRate);

            // Логика состояний
            // Обычный бег активен при любом движении без режима ходьбы
            // Ускоренный бег (спринт) — отдельный флаг поверх обычного бега
            _isWalking = hasInput && _isWalkingMode;
            _isRunning = hasInput && !_isWalkingMode;
            _isFastRunning = hasInput && !_isWalkingMode && isSprintingInput;
            if (_isFastRunning)
            {
                _hadSprint = true;
            }

            // Если скорости почти нет, обнуляем её совсем и сбрасываем флаг спринта
            if (!hasInput && _speed < 0.1f)
            {
                _speed = 0f;
                _hadSprint = false;
            }

            Vector3 move = Vector3.zero;

            if (hasInput || _speed > 0.1f)
            {
                Vector3 inputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;

                if (hasInput)
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                move = targetDirection.normalized * (_speed * Time.deltaTime);
            }

            // Всегда применяем вертикальную скорость (гравитация и прыжок), иначе в воздухе застреваем
            move += new Vector3(0.0f, _verticalVelocity * Time.deltaTime, 0.0f);
            _controller.Move(move);
        }

        private void Jump()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            _isGrounded = Physics.CheckSphere(spherePosition, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;
                if (_jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _jump = false;
                }
                if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jump = false;
                _jumpTimeoutDelta = JumpTimeout;
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
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

        private void OnDisable() => _inputActions.Disable();
    }
}