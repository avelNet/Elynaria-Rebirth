using UnityEngine;

namespace StarterAssets
{
    public class TPSVisual : MonoBehaviour
    {
        // MoveState: 0=Idle, 1=Walking, 2=Running, 3=Sprinting
        private static readonly int MoveStateHash = Animator.StringToHash("MoveState");
        // JumpState: 0=None, 1=IdleJump, 2=WalkJump, 3=RunJump, 4=SprintJump
        private static readonly int JumpStateHash = Animator.StringToHash("JumpState");

        private Animator _animator;
        private ThirdPersonController _controller;

        private float _landingDebounceTime = 0.1f;
        private float _lastLandingTime = -1f;

        private void Awake()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _animator = GetComponent<Animator>();
            _controller = GetComponentInParent<ThirdPersonController>();
        }

        private void Update()
        {
            if (_animator == null) return;
            UpdateMoveState();
            UpdateJumpState();
        }

        private void UpdateMoveState()
        {
            int state = 0;
            if (_controller.IsSprinting())       state = 3;
            else if (_controller.IsRunning())    state = 2;
            else if (_controller.IsWalking())    state = 1;

            _animator.SetInteger(MoveStateHash, state);
        }

        private void UpdateJumpState()
        {
            bool isGrounded = _controller.IsGrounded();

            if (isGrounded)
            {
                _lastLandingTime = Time.time;
                _animator.SetInteger(JumpStateHash, 0);
                return;
            }

            bool pastDebounce = (Time.time - _lastLandingTime) > _landingDebounceTime;
            if (!pastDebounce)
            {
                _animator.SetInteger(JumpStateHash, 0);
                return;
            }

            if (_controller.WasSprintingAtJumpStart())      _animator.SetInteger(JumpStateHash, 4);
            else if (_controller.WasRunningAtJumpStart())   _animator.SetInteger(JumpStateHash, 3);
            else if (_controller.WasWalkingAtJumpStart())   _animator.SetInteger(JumpStateHash, 2);
            else                                            _animator.SetInteger(JumpStateHash, 1);
        }
    }
}
