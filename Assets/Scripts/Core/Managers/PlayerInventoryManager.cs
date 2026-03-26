// ──────────────────────────────────────────────────────────────────────────────
// PlayerInventoryManager.cs
// Manages the visual rock stack on the player's back and tracks inventory state.
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeIdle.Core
{
    /// <summary>
    /// Manages the player's carried‑item stack.
    /// Attach this MonoBehaviour to the Player GameObject.
    /// Items are visually stacked on the player's back and deposited at a processor.
    /// </summary>
    public class PlayerInventoryManager : MonoBehaviour
    {
        #region ── Serialized Fields ──────────────────────────────────────────

        [Header("Stack Settings")]
        [Tooltip("Transform on the player's back where the bottom‑most item sits.")]
        [SerializeField] private Transform _stackAnchor;

        [Tooltip("Local vertical offset applied between each stacked item.")]
        [SerializeField] private Vector3 _stackOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Capacity")]
        [Tooltip("Maximum number of items the player can carry at once.")]
        [SerializeField] private int _maxCapacity = 10;

        [Header("Runtime State (read‑only in Inspector)")]
        [Tooltip("Currently stacked visual item GameObjects.")]
        [SerializeField] private List<GameObject> _stackedItems = new List<GameObject>();

        #endregion

        #region ── Events / Actions ───────────────────────────────────────────

        /// <summary>Fires when an item is added to the stack. Passes the new count.</summary>
        public event Action<int> OnItemAdded;

        /// <summary>Fires when an item is removed from the stack. Passes the new count.</summary>
        public event Action<int> OnItemRemoved;

        /// <summary>Fires when the stack reaches maximum capacity.</summary>
        public event Action OnCapacityReached;

        #endregion

        #region ── Public Read‑Only Properties ────────────────────────────────

        /// <summary>Number of items currently on the stack.</summary>
        public int CurrentCount => _stackedItems.Count;

        /// <summary>Current maximum number of items the player can carry.</summary>
        public int MaxCapacity => _maxCapacity;

        /// <summary><c>true</c> when the stack has reached <see cref="MaxCapacity"/>.</summary>
        public bool IsFull => _stackedItems.Count >= _maxCapacity;

        /// <summary><c>true</c> when the stack contains no items.</summary>
        public bool IsEmpty => _stackedItems.Count == 0;

        #endregion

        #region ── Unity Lifecycle ────────────────────────────────────────────

        private void Awake()
        {
            // TODO: Validate serialized references (_stackAnchor != null, _maxCapacity > 0).
            // TODO: Pre‑allocate _stackedItems list capacity if desired.
        }

        #endregion

        #region ── Public Methods ─────────────────────────────────────────────

        /// <summary>
        /// Adds a visual item to the top of the stack and parents it to the stack anchor.
        /// The item is positioned via <see cref="GetNextStackPosition"/> before being added.
        /// Fires <see cref="OnItemAdded"/> and, if the stack is now full,
        /// <see cref="OnCapacityReached"/>.
        /// </summary>
        /// <param name="visualItem">
        /// The <see cref="GameObject"/> that represents the mined rock visual.
        /// </param>
        public void AddItem(GameObject visualItem)
        {
            // TODO: Guard – return early if visualItem is null or stack is already full.
            // TODO: Parent visualItem to _stackAnchor.
            // TODO: Position visualItem at GetNextStackPosition().
            // TODO: Add visualItem to _stackedItems.
            // TODO: Invoke OnItemAdded with new count.
            // TODO: If IsFull, invoke OnCapacityReached.
        }

        /// <summary>
        /// Removes the top‑most item from the stack and returns it (e.g. for depositing
        /// into a processor). Calls <see cref="RefreshStackPositions"/> afterward and
        /// fires <see cref="OnItemRemoved"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="GameObject"/> that was on top of the stack,
        /// or <c>null</c> if the stack was empty.
        /// </returns>
        public GameObject RemoveItemFromTop()
        {
            // TODO: Guard – return null if stack is empty.
            // TODO: Cache reference to the last item in _stackedItems.
            // TODO: Remove that item from _stackedItems.
            // TODO: Un‑parent the item (set parent to null).
            // TODO: Call RefreshStackPositions().
            // TODO: Invoke OnItemRemoved with new count.
            // TODO: Return the removed GameObject.

            return null; // Placeholder
        }

        /// <summary>
        /// Removes and returns every item on the stack (bulk deposit).
        /// Fires <see cref="OnItemRemoved"/> once with a count of <c>0</c>.
        /// </summary>
        /// <returns>
        /// A new <see cref="List{T}"/> containing all previously stacked
        /// <see cref="GameObject"/> instances. Returns an empty list if the stack
        /// was already empty.
        /// </returns>
        public List<GameObject> RemoveAllItems()
        {
            // TODO: Create a copy of _stackedItems.
            // TODO: Un‑parent every item in the copy.
            // TODO: Clear _stackedItems.
            // TODO: Invoke OnItemRemoved with count 0.
            // TODO: Return the copy.

            return new List<GameObject>(); // Placeholder
        }

        /// <summary>
        /// Upgrades the maximum carrying capacity.
        /// Does <b>not</b> remove items if the current count already exceeds the new max.
        /// </summary>
        /// <param name="newMax">The new maximum capacity (must be greater than zero).</param>
        public void UpgradeCapacity(int newMax)
        {
            // TODO: Validate newMax > 0.
            // TODO: Set _maxCapacity to newMax.
            // TODO: Optionally fire an OnCapacityUpgraded event (extend as needed).
        }

        /// <summary>
        /// Calculates the world‑space position where the next item should be placed
        /// on top of the current stack.
        /// </summary>
        /// <returns>World‑space <see cref="Vector3"/> for the next stack slot.</returns>
        public Vector3 GetNextStackPosition()
        {
            // TODO: Return _stackAnchor.position + (_stackOffset * CurrentCount).
            // TODO: Account for the anchor's rotation if the offset should be in local space.

            return Vector3.zero; // Placeholder
        }

        /// <summary>
        /// Re‑positions every item in the stack so there are no visual gaps.
        /// Call this after removing an item from the middle or top of the stack.
        /// </summary>
        public void RefreshStackPositions()
        {
            // TODO: Iterate through _stackedItems.
            // TODO: Set each item's position to _stackAnchor.position + (_stackOffset * index).
        }

        #endregion
    }
}
