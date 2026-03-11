using UnityEngine;

namespace StarterAssets
{
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _rotationSmoothTime = 0.12f;
        private bool _isRunning;

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

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _mainCamera = Camera.main.transform;

            _inputActions = new PlayerInputSystem();
            _inputActions.Enable();
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();
            Vector3 targetDirection = Vector3.zero;

            if (input.sqrMagnitude > 0.01f)
            {
                // Угол для поворота игрока в нужную сторону
                _targetRotation = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _mainCamera.eulerAngles.y;

                // Вращение в сторону куда смотрит камера
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                // Направление для джижения
                targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                _isRunning = true;
            }
            else
            {
                _isRunning = false;
            }

            Vector3 move = targetDirection * (_moveSpeed * Time.deltaTime);
            _controller.Move(move);
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
    }
}