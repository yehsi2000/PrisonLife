using UnityEngine;

namespace ArcadeIdle.Player
{
    /// <summary>
    /// Central MonoBehaviour that lives on the Player GameObject.
    /// Handles movement (virtual joystick input), interaction zone detection,
    /// mining trigger, and references to sub-components like <see cref="ArcadeIdle.Core.PlayerInventoryManager"/>.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement")]
        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Rotation smoothing factor for turning toward movement direction.")]
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("Component References")]
        [Tooltip("Reference to the PlayerInventoryManager on this GameObject.")]
        [SerializeField] private ArcadeIdle.Core.PlayerInventoryManager _inventory;

        #endregion

        #region Public Properties

        /// <summary>The player's inventory (rock stack) manager.</summary>
        public ArcadeIdle.Core.PlayerInventoryManager Inventory => _inventory;

        /// <summary><c>true</c> when the player is actively moving via joystick input.</summary>
        public bool IsMoving { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // TODO: Cache required component references.
            // TODO: Validate _inventory is assigned.
        }

        private void Update()
        {
            // TODO: Read joystick input.
            // TODO: Apply movement via CharacterController or Rigidbody.
            // TODO: Rotate toward movement direction.
            // TODO: Update IsMoving state.
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the player to stop moving (e.g., during cutscenes or pauses).
        /// </summary>
        public void StopMovement()
        {
            // TODO: Zero out velocity, set IsMoving = false.
        }

        /// <summary>
        /// Resumes player movement after a forced stop.
        /// </summary>
        public void ResumeMovement()
        {
            // TODO: Re-enable input processing.
        }

        #endregion
    }
}
