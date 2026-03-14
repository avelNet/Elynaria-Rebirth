using UnityEngine;

namespace StarterAssets
{
    public class TPSVisual : MonoBehaviour
    {
        private Animator _animator;
        private ThirdPersonController _controller;

        [Header("Animator Parametrs")]
        private bool _isRunning;
        private bool _isWalking;
        private bool _isSprinting;
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

            bool isGrounded = _controller.IsGrounded();

            if (isGrounded)
            {
                RunningAnimations();
                SprintAnimation();
                WalkingAnimation();

                // Сбрасываем прыжковые параметры только когда приземлились
                _animator.SetBool("SprintJumping", false);
                _animator.SetBool("RunJumping", false);
                _animator.SetBool("WalkingJumping", false);
                _animator.SetBool("IdleJumping", false);
            }
            else
            {
                // НЕ выключайте isSprinting здесь сразу, если он нужен для условий перехода
                JumpingAnimation();
            }
        }

        private void RunningAnimations()
        {
            _isRunning = _controller.IsRunning();
            if (_isRunning)
            {
                _animator.SetBool("isRunning", _isRunning);
            }
            else
            {
                _animator.SetBool("isRunning", false);
            }
        }

        private void WalkingAnimation()
        {
            _isWalking = _controller.IsWalking();
            if (_isWalking)
            {
                _animator.SetBool("isWalking", _isWalking);
            }
            else
            {
                _animator.SetBool("isWalking", false);
            }
        }

        private void SprintAnimation()
        {
            _isSprinting = _controller.IsSprinting();
            _animator.SetBool("isSprinting", _isSprinting);
        }

        private void JumpingAnimation()
        {
            bool isGrounded = _controller.IsGrounded();

            if (isGrounded)
            {
                _lastLandingTime = Time.time;

                _animator.SetBool("IdleJumping", false);
                _animator.SetBool("WalkingJumping", false);
                _animator.SetBool("RunJumping", false);
                _animator.SetBool("SprintJumping", false);
            }
            else
            {
                bool pastLandingDebounce = (Time.time - _lastLandingTime) > _landingDebounceTime;

                if (!pastLandingDebounce) return;

                bool wasSprintingAtJump = _controller.WasSprintingAtJumpStart();
                bool wasRunningAtJump = _controller.WasRunningAtJumpStart();
                bool wasWalkingAtJump = _controller.WasWalkingAtJumpStart();

                bool wasIdleAtJump = !wasRunningAtJump && !wasWalkingAtJump && !wasSprintingAtJump;

                _animator.SetBool("SprintJumping", wasSprintingAtJump);
                _animator.SetBool("RunJumping", wasRunningAtJump);
                _animator.SetBool("WalkingJumping", wasWalkingAtJump);
                _animator.SetBool("IdleJumping", wasIdleAtJump);
            }
        }
    }
}