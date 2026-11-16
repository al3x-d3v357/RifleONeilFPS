using UnityEngine;
using UnityEngine.InputSystem;

namespace RifleONeilFPS
{
    /// <summary>
    /// First-person movement, sprint, jump, and mouse-look controller with ADS sensitivity scaling.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 7.2f;
        public float sprintMultiplier = 1.45f;
        public float jumpHeight = 1.35f;
        public float gravity = -19.6f;

        [Header("Mouse Look")]
        public Transform cameraTransform;
        public float mouseSensitivity = 0.14f;
        public float adsSensitivityMultiplier = 0.55f;
        public float maxLookAngle = 82.0f;

        public bool IsMoving { get; private set; }
        public float CurrentSpeedRatio { get; private set; }

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _pitch;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            LockCursor(true);
        }

        private void OnDisable()
        {
            LockCursor(false);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }

            bool isAiming = mouse.rightButton.isPressed;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                HandleLook(mouse, isAiming ? mouseSensitivity * adsSensitivityMultiplier : mouseSensitivity);
            }

            HandleMovement(kb);
        }

        private void HandleLook(Mouse mouse, float sensitivity)
        {
            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            transform.Rotate(Vector3.up * delta.x);

            _pitch = Mathf.Clamp(_pitch - delta.y, -maxLookAngle, maxLookAngle);
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);
            }
        }

        private void HandleMovement(Keyboard kb)
        {
            float h = 0.0f;
            float v = 0.0f;
            if (kb.dKey.isPressed) h += 1.0f;
            if (kb.aKey.isPressed) h -= 1.0f;
            if (kb.wKey.isPressed) v += 1.0f;
            if (kb.sKey.isPressed) v -= 1.0f;

            Vector3 move = (transform.right * h + transform.forward * v).normalized;
            bool sprinting = kb.leftShiftKey.isPressed && v > 0.1f;
            float speed = sprinting ? moveSpeed * sprintMultiplier : moveSpeed;

            IsMoving = move.sqrMagnitude > 0.01f && _controller.isGrounded;
            CurrentSpeedRatio = IsMoving ? (sprinting ? 1.4f : 1.0f) : 0.0f;

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0.0f) _verticalVelocity = -2.0f;
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                }
            }

            _verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = move * speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
