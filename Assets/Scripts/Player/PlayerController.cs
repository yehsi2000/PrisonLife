using UnityEngine;
using ArcadeIdle.Input;
using ArcadeIdle.Core;

namespace ArcadeIdle.Player
{
    /// <summary>
    /// Central MonoBehaviour that lives on the Player GameObject.
    /// Drives CharacterController-based movement from a UI FloatingJoystick,
    /// rotates the character to face the movement direction, applies gravity,
    /// and exposes sub-component references (inventory, etc.).
    /// <para>
    /// Required components on the same GameObject:
    /// <see cref="CharacterController"/>, <see cref="PlayerInventoryManager"/>.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input")]
        [Tooltip("Reference to the FloatingJoystick UI component in the scene.")]
        [SerializeField] private FloatingJoystick _joystick;

        [Header("Movement")]
        [Tooltip("Maximum movement speed in units per second.")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Gravity applied to the character every frame (positive value, applied downward).")]
        [SerializeField] private float _gravity = 20f;

        [Header("Rotation")]
        [Tooltip("How fast the character rotates toward the movement direction (degrees per second via Slerp smoothing factor).")]
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("Component References")]
        [Tooltip("Reference to the PlayerInventoryManager on this GameObject.")]
        [SerializeField] private PlayerInventoryManager _inventory;

        #endregion

        #region Private Fields

        /// <summary>Cached CharacterController component.</summary>
        private CharacterController _characterController;

        /// <summary>Current vertical velocity (used for gravity accumulation).</summary>
        private float _verticalVelocity;

        /// <summary>When false, input is ignored and the character stands still.</summary>
        private bool _canMove = true;

        #endregion

        #region Public Properties

        /// <summary>The player's inventory (rock stack) manager.</summary>
        public PlayerInventoryManager Inventory => _inventory;

        /// <summary><c>true</c> when the player is actively moving via joystick input.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>The cached <see cref="CharacterController"/> on this GameObject.</summary>
        public CharacterController CharacterController => _characterController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache the required CharacterController
            _characterController = GetComponent<CharacterController>();

            // Validate critical references
            if (_joystick == null)
                Debug.LogError($"[PlayerController] FloatingJoystick reference is not assigned on '{gameObject.name}'.", this);

            if (_inventory == null)
            {
                _inventory = GetComponent<PlayerInventoryManager>();
                if (_inventory == null)
                    Debug.LogWarning($"[PlayerController] PlayerInventoryManager not found on '{gameObject.name}'.", this);
            }
        }

        private void Update()
        {
            HandleMovement();
        }

        #endregion

        #region Movement Logic

        /// <summary>
        /// Core movement method called every frame.
        /// Reads joystick input, builds a world-space movement vector,
        /// applies gravity, moves via CharacterController, and rotates
        /// the character to face the movement direction.
        /// </summary>
        private void HandleMovement()
        {
            // --- Early exit if movement is disabled ---
            if (!_canMove || _joystick == null)
            {
                ApplyGravity();
                IsMoving = false;
                return;
            }

            // --- Read joystick input ---
            // Joystick.Horizontal maps to X (left/right)
            // Joystick.Vertical maps to Z (forward/back) in a top-down view
            float horizontal = _joystick.Horizontal;
            float vertical = _joystick.Vertical;

            // Build the movement direction on the XZ plane
            Vector3 moveDirection = new Vector3(horizontal, 0f, vertical);

            // Determine if there is meaningful input
            // Use sqrMagnitude to avoid a sqrt — compare against a small epsilon squared
            bool hasInput = moveDirection.sqrMagnitude > 0.01f;
            IsMoving = hasInput;

            // --- Calculate horizontal displacement ---
            Vector3 displacement = Vector3.zero;

            if (hasInput)
            {
                // Normalize to prevent diagonal speed boost, then scale by speed.
                // Multiply by the raw magnitude (clamped to 1 by the joystick) so that
                // partial tilts produce proportionally slower movement.
                float inputMagnitude = Mathf.Clamp01(moveDirection.magnitude);
                Vector3 normalizedDirection = moveDirection.normalized;

                displacement = normalizedDirection * (_moveSpeed * inputMagnitude * Time.deltaTime);

                // --- Rotate toward movement direction ---
                HandleRotation(normalizedDirection);
            }

            // --- Apply gravity ---
            if (_characterController.isGrounded)
            {
                // Small downward force to keep the controller "stuck" to the ground
                _verticalVelocity = -0.5f;
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            displacement.y = _verticalVelocity * Time.deltaTime;

            // --- Move the CharacterController ---
            _characterController.Move(displacement);
        }

        /// <summary>
        /// Smoothly rotates the character to face the given world-space direction
        /// using <see cref="Quaternion.Slerp"/> for a polished, non-snappy feel.
        /// </summary>
        /// <param name="direction">Normalized XZ movement direction to face.</param>
        private void HandleRotation(Vector3 direction)
        {
            // Build the target rotation from the direction vector
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            // Smoothly interpolate from current rotation toward the target
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Applies gravity when movement is disabled (so the character doesn't float).
        /// Called from <see cref="HandleMovement"/> on early-exit paths.
        /// </summary>
        private void ApplyGravity()
        {
            if (_characterController == null) return;

            if (_characterController.isGrounded)
            {
                _verticalVelocity = -0.5f;
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            _characterController.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the player to stop moving. Input is ignored until
        /// <see cref="ResumeMovement"/> is called. Useful for cutscenes,
        /// pause menus, or interaction locks.
        /// </summary>
        public void StopMovement()
        {
            _canMove = false;
            IsMoving = false;
            _verticalVelocity = 0f;
        }

        /// <summary>
        /// Re-enables movement after a <see cref="StopMovement"/> call.
        /// </summary>
        public void ResumeMovement()
        {
            _canMove = true;
        }

        /// <summary>
        /// Sets the movement speed at runtime (e.g., from a speed boost power-up).
        /// </summary>
        /// <param name="speed">New movement speed in units per second. Clamped to >= 0.</param>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Assigns the joystick reference at runtime (e.g., if the UI is instantiated dynamically).
        /// </summary>
        /// <param name="joystick">The <see cref="FloatingJoystick"/> to bind.</param>
        public void SetJoystick(FloatingJoystick joystick)
        {
            _joystick = joystick;
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        /// <summary>
        /// Called in the Editor when the script is loaded or a value changes in the Inspector.
        /// Validates that a CharacterController exists on this GameObject.
        /// </summary>
        private void OnValidate()
        {
            if (_moveSpeed < 0f) _moveSpeed = 0f;
            if (_gravity < 0f) _gravity = 0f;
            if (_rotationSpeed < 0f) _rotationSpeed = 0f;
        }
#endif

        #endregion
    }
}
