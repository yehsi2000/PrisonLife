using UnityEngine;
using UnityEngine.AI;
using PrisonLife.Input;
using PrisonLife.Core;

namespace PrisonLife.Player
{
    /// <summary>
    /// Central MonoBehaviour that lives on the Player GameObject.
    /// Drives <see cref="NavMeshAgent"/>-based movement from a UI FloatingJoystick,
    /// constraining the player to the baked NavMesh surface while retaining
    /// full manual control over speed and rotation.
    /// <para>
    /// Required components on the same GameObject:
    /// <see cref="NavMeshAgent"/>, <see cref="PlayerInventoryManager"/>.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input")]
        [Tooltip("Reference to the FloatingJoystick UI component in the scene.")]
        [SerializeField] private FloatingJoystick _joystick;

        [Header("Movement")]
        [Tooltip("Maximum movement speed in units per second.")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Component References")]
        [Tooltip("Reference to the PlayerInventoryManager on this GameObject.")]
        [SerializeField] private PlayerInventoryManager _inventory;

        #endregion

        #region Private Fields

        /// <summary>Cached NavMeshAgent component.</summary>
        private NavMeshAgent _agent;

        /// <summary>When false, input is ignored and the character stands still.</summary>
        private bool _canMove = true;

        #endregion

        #region Public Properties

        /// <summary>The player's inventory (rock stack) manager.</summary>
        public PlayerInventoryManager Inventory => _inventory;

        /// <summary><c>true</c> when the player is actively moving via joystick input.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>The cached <see cref="NavMeshAgent"/> on this GameObject.</summary>
        public NavMeshAgent Agent => _agent;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache the required NavMeshAgent
            _agent = GetComponent<NavMeshAgent>();

            // Disable automatic pathfinding — we drive movement manually via Move().
            // The agent still constrains the resulting position to the NavMesh surface.
            _agent.updateRotation = false;  // we handle rotation ourselves
            _agent.updateUpAxis   = false;  // keep agent flat on the surface
            _agent.speed          = 0f;     // no automatic movement; we call Move() directly

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
        /// Reads joystick input, builds a world-space displacement vector,
        /// passes it to <see cref="NavMeshAgent.Move"/> (which constrains it
        /// to the NavMesh), and rotates the character to face the direction.
        /// </summary>
        private void HandleMovement()
        {
            // --- Early exit if movement is disabled ---
            if (!_canMove || _joystick == null)
            {
                IsMoving = false;
                return;
            }

            // --- Read joystick input ---
            // Joystick.Horizontal maps to X (left/right)
            // Joystick.Vertical   maps to Z (forward/back) in a top-down view
            float horizontal = _joystick.Horizontal;
            float vertical   = _joystick.Vertical;

            // Build the movement direction on the XZ plane
            Vector3 moveDirection = new Vector3(horizontal, 0f, vertical);

            // Determine if there is meaningful input
            // Use sqrMagnitude to avoid a sqrt — compare against a small epsilon squared
            bool hasInput = moveDirection.sqrMagnitude > 0.01f;
            IsMoving = hasInput;

            if (hasInput)
            {
                // Normalize to prevent diagonal speed boost, then scale by speed.
                // Multiply by the raw magnitude (clamped to 1 by the joystick) so that
                // partial tilts produce proportionally slower movement.
                float inputMagnitude     = Mathf.Clamp01(moveDirection.magnitude);
                Vector3 normalizedDirection = moveDirection.normalized;

                // NavMeshAgent.Move() accepts a displacement vector (same API as
                // CharacterController.Move) but automatically clamps the result to
                // the baked NavMesh — the player cannot walk off walkable surfaces.
                Vector3 displacement = normalizedDirection * (_moveSpeed * inputMagnitude * Time.deltaTime);
                _agent.Move(displacement);

                // --- Rotate toward movement direction ---
                HandleRotation(normalizedDirection);
            }
        }

        /// <summary>
        /// Instantly rotates the character to face the given world-space direction.
        /// </summary>
        /// <param name="direction">Normalized XZ movement direction to face.</param>
        private void HandleRotation(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
            _canMove  = false;
            IsMoving  = false;
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
        /// </summary>
        private void OnValidate()
        {
            if (_moveSpeed < 0f) _moveSpeed = 0f;
        }
#endif

        #endregion
    }
}
