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
        [SerializeField] private float _rotationSmoothTime = 0.12f;
        private float _speedChangeRate = 10.0f;
        private float _targetSpeed;
        private bool _isRunning;
        private bool _isRunMode;
        private bool _isWalking;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -15.0f;
        private float _verticalVelocity;

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

        private float _currentSpeed;
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
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.WalkToggle.performed -= WalkToggle_performed;
            _inputActions.Disable();
        }

        private void WalkToggle_performed(InputAction.CallbackContext context)
        {
            _isRunMode = !_isRunMode;
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();
            bool isSprinting = _inputActions.Player.Sprint.IsPressed();

            ApplyGravity();
            Vector3 targetDirection = Vector3.zero;

            if (input.sqrMagnitude > 0.01f)
            {
                float targetSpeed;
                if(_isRunMode)
                {
                    if(_shiftInput > 0.5f)
                    {
                        targetSpeed = _sprintSpeed;
                    }
                    else
                    {
                        targetSpeed = _runningSpeed;
                    }
                }
                else
                {
                    targetSpeed = _moveSpeed;
                    _isWalking = true;
                }

                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * _speedChangeRate);

                    // Угол для поворота игрока в нужную сторону
                _targetRotation = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _mainCamera.eulerAngles.y;

                // Вращение в сторону куда смотрит камера
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                // Направление для джижения
                targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                _isRunning = _currentSpeed > _moveSpeed + 0.1f;
            }
            else
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, _speedChangeRate * Time.deltaTime);
                _isRunning = false;
                _isWalking = false;
            }

            Vector3 move = targetDirection * (_currentSpeed * Time.deltaTime);
            move.y = _verticalVelocity * Time.deltaTime;
            _controller.Move(move);
        }

        private void ApplyGravity()
        {
            if(_controller.isGrounded)
            {
                if(_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }
            }
            else
            {
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
        public bool IsRunning()
        {
            return _isRunning;
        }

        public bool IsWalking()
        {
            return _isWalking;
        }
    }
}