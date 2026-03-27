// ──────────────────────────────────────────────────────────────────────────────
// PlayerInventoryManager.cs
// Manages the visual item stack the player holds and tracks inventory state.
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace PrisonLife.Core
{
    /// <summary>
    /// Manages the player's carried‑item stack.
    /// Attach this MonoBehaviour to the Player GameObject.
    /// Items are visually stacked on the player's back and deposited at a processor.
    /// </summary>
    public class PlayerInventoryManager : MonoBehaviour
    {
        #region ── Serialized Fields ──────────────────────────────────────────

        [Serializable]
        public class ItemInventoryData
        {
            [Tooltip("Transform following player where the bottom‑most item sits.")]
            [SerializeField]
            private Transform _stackAnchor;

            [Tooltip("Local vertical offset applied between each stacked item.")]
            [SerializeField]
            private Vector3 _stackOffset = new Vector3(0f, 0.5f, 0f);

            [Tooltip("Maximum number of items the player can carry at once.")]
            [SerializeField]
            private int _maxCapacity = 10;

            [SerializeField]
            private List<GameObject> _stackedItems = new List<GameObject>();

            public List<GameObject> StackedItems => _stackedItems;

            /// <summary>Number of items currently on the stack.</summary>
            public int CurrentCount => StackedItems.Count;
            /// <summary>Current maximum number of items the player can carry.</summary>
            public int MaxCapacity => _maxCapacity;
            /// <summary><c>true</c> when the stack has reached <see cref="MaxCapacity"/>.</summary>
            public bool IsFull => StackedItems.Count >= _maxCapacity;
            /// <summary><c>true</c> when the stack contains no items.</summary>
            public bool IsEmpty => StackedItems.Count == 0;

            #region ── Events / Actions ───────────────────────────────────────────

            /// <summary>Fires when an item is added to the stack. Passes the new count.</summary>
            public event Action<int> OnItemAdded;

            /// <summary>Fires when an item is removed from the stack. Passes the new count.</summary>
            public event Action<int> OnItemRemoved;

            /// <summary>Fires when the stack reaches maximum capacity.</summary>
            public event Action OnCapacityReached;

            #endregion

            #region ── Public Methods ─────────────────────────────────────────────

            /// <summary>
            /// Adds a visual item to the top of the stack and parents it to the stack anchor.
            /// The item is positioned via <see cref="GetNextStackPosition"/> before being added.
            /// Fires <see cref="OnItemAdded"/> and, if the stack is now full,
            /// <see cref="OnCapacityReached"/>.
            /// </summary>
            /// <param name="visualItem">
            /// The <see cref="GameObject"/> that represents the item visual.
            /// </param>
            public void AddItem(GameObject visualItem)
            {
                if (visualItem == null || IsFull) return;

                visualItem.transform.SetParent(_stackAnchor);
                visualItem.transform.position = GetNextStackPosition();
                visualItem.transform.localRotation = Quaternion.identity;

                _stackedItems.Add(visualItem);
                OnItemAdded?.Invoke(CurrentCount);

                if (IsFull)
                    OnCapacityReached?.Invoke();
            }

            /// <summary>
            /// Removes the top‑most item from the stack and returns it (e.g. for depositing
            /// into a processor). Fires <see cref="OnItemRemoved"/>.
            /// </summary>
            /// <returns>
            /// The <see cref="GameObject"/> that was on top of the stack,
            /// or <c>null</c> if the stack was empty.
            /// </returns>
            public GameObject RemoveItemFromTop()
            {
                if (IsEmpty) return null;
                int lastIndex = _stackedItems.Count - 1;
                GameObject top = _stackedItems[lastIndex];
                _stackedItems.RemoveAt(lastIndex);
                top.transform.SetParent(null);
                OnItemRemoved?.Invoke(CurrentCount);
                return top;
            }

            /// <summary>
            /// Removes every item on the stack.
            /// Fires <see cref="OnItemRemoved"/> once with a count of <c>0</c>.
            /// </summary>
            public void RemoveAllItems()
            {
                while (_stackedItems.Count > 0)
                {
                    int lastIndex = _stackedItems.Count - 1;
                    var item = _stackedItems[lastIndex];
                    _stackedItems.RemoveAt(lastIndex);
                    item.transform.SetParent(null);
                }
                OnItemRemoved?.Invoke(0);
            }

            /// <summary>
            /// Calculates the world‑space position where the next item should be placed
            /// on top of the current stack.
            /// </summary>
            /// <returns>World‑space <see cref="Vector3"/> for the next stack slot.</returns>
            public Vector3 GetNextStackPosition()
            {
                return _stackAnchor.position + _stackAnchor.TransformDirection(_stackOffset * CurrentCount);
            }

            /// <summary>
            /// Re‑positions every item in the stack so there are no visual gaps.
            /// Call this after removing an item from the middle or top of the stack.
            /// </summary>
            public void RefreshStackPositions()
            {
                for (int i = 0; i < _stackedItems.Count; i++)
                {
                    _stackedItems[i].transform.position = _stackAnchor.position + _stackAnchor.TransformDirection(_stackOffset * i);
                }
            }

            /// <summary>
            /// Upgrades the maximum carrying capacity.
            /// Does <b>not</b> remove items if the current count already exceeds the new max.
            /// </summary>
            /// <param name="newMax">The new maximum capacity (must be greater than zero).</param>
            public void UpgradeCapacity(int newMax)
            {
                if (newMax > 0)
                    _maxCapacity = newMax;
            }

            #endregion
        }


        [Header("Item Data Settings")]

        [SerializeField] private ItemInventoryData handcuffData;
        [SerializeField] private ItemInventoryData oreData;
        [SerializeField] private ItemInventoryData moneyData;

        #endregion

        #region ── Public Properties ─────────────────────────────────────────────

        /// <summary>The player's handcuff inventory data.</summary>
        public ItemInventoryData Handcuffs => handcuffData;

        /// <summary>The player's ore inventory data.</summary>
        public ItemInventoryData Ore => oreData;

        /// <summary>The player's money inventory data.</summary>
        public ItemInventoryData Money => moneyData;

        #endregion

        #region ── Unity Lifecycle ────────────────────────────────────────────

        private void Awake()
        {
            Assert.IsNotNull(handcuffData);
            Assert.IsNotNull(oreData);
            Assert.IsNotNull(moneyData);
            Assert.IsTrue(handcuffData.MaxCapacity > 0);
            Assert.IsTrue(oreData.MaxCapacity > 0);
            Assert.IsTrue(moneyData.MaxCapacity > 0);
        }

        #endregion
    }
}
