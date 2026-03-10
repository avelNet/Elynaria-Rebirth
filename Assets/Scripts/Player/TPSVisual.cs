using UnityEngine;

namespace StarterAssets
{
    public class TPSVisual : MonoBehaviour
    {
        private Animator _animator;
        private ThirdPersonController _controller;

        private void Awake()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _animator = GetComponent<Animator>();
            _controller = GetComponentInParent<ThirdPersonController>();
        }

        private void Update()
        {
            if (_controller == null) return;

            _animator.SetBool("isWalking", _controller.IsWalking);
            _animator.SetBool("isRunning", _controller.IsRunning);
            _animator.SetBool("isFastRunning", _controller.IsFastRunning);
            _animator.SetBool("isStopping", _controller.IsStopping);

            _animator.SetFloat("Speed", _controller.CurrentSpeed, 0.1f, Time.deltaTime);
        }
    }
}