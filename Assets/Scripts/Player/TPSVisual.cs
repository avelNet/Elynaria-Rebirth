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
            if(_animator != null )
            {
                RunningAnimations();
            }
        }

        private void RunningAnimations()
        {
            bool _isRunning = _controller.IsRunning();
            if(_isRunning)
            {
                _animator.SetBool("isRunning", _isRunning);
            }
            else
            {
                _animator.SetBool("isRunning", false);
            }
        }
    }
}