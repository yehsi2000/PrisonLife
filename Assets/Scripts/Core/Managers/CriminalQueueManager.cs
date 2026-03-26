using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeIdle.Core
{
    /// <summary>
    /// Manages an infinite queue of criminals waiting at a desk.
    /// Criminals absorb handcuffs from the output desk, and once their requirement
    /// is met they transform into prisoners and walk to a jail cell.
    /// The queue is never empty — a new criminal spawns at the back whenever one leaves.
    /// </summary>
    public class CriminalQueueManager : MonoBehaviour
    {
        #region Singleton

        public static CriminalQueueManager Instance { get; private set; }

        #endregion

        // ──────────────────────────────────────────────
        #region Serialized Fields
        // ──────────────────────────────────────────────

        [Header("Queue Layout")]
        [Tooltip("World position of the front of the queue (at the desk).")]
        [SerializeField] private Transform _queueStartPoint;

        [Tooltip("Distance between each criminal in the line.")]
        [SerializeField] private float _queueSpacing = 1.5f;

        [Tooltip("Normalized direction the queue extends (e.g., back along -Z).")]
        [SerializeField] private Vector3 _queueDirection = Vector3.back;

        [Header("Spawning")]
        [Tooltip("How many criminals to spawn when the game starts.")]
        [SerializeField] private int _initialQueueSize = 5;

        [Tooltip("Reference to the criminal prefab (supports pooling).")]
        [SerializeField] private GameObject _criminalPrefab;

        [Header("Handcuff Requirements")]
        [Tooltip("Minimum random handcuff requirement for a criminal.")]
        [SerializeField] private int _minHandcuffRequirement = 1;

        [Tooltip("Maximum random handcuff requirement for a criminal.")]
        [SerializeField] private int _maxHandcuffRequirement = 5;

        [Header("Jail")]
        [Tooltip("World position where satisfied criminals walk to after changing into prisoner uniform.")]
        [SerializeField] private Transform _jailTargetPoint;

        #endregion

        // ──────────────────────────────────────────────
        #region Private Fields
        // ──────────────────────────────────────────────

        /// <summary>
        /// Ordered queue of active criminals. LinkedList is used for O(1)
        /// removal at the front and insertion at the back.
        /// </summary>
        private LinkedList<CriminalController> _criminalQueue = new LinkedList<CriminalController>();

        #endregion

        // ──────────────────────────────────────────────
        #region Events / Actions
        // ──────────────────────────────────────────────

        /// <summary>Fires when the front criminal's handcuff requirement is fully met.</summary>
        public event Action<CriminalController> OnCriminalSatisfied;

        /// <summary>Fires after the entire queue shifts forward by one slot.</summary>
        public event Action OnQueueAdvanced;

        /// <summary>Fires when a new criminal is spawned and added to the back of the queue.</summary>
        public event Action<CriminalController> OnNewCriminalSpawned;

        #endregion

        // ──────────────────────────────────────────────
        #region Public Properties
        // ──────────────────────────────────────────────

        /// <summary>
        /// The criminal currently at the front of the queue (at the desk).
        /// Returns <c>null</c> if the queue is unexpectedly empty.
        /// </summary>
        public CriminalController FrontCriminal =>
            _criminalQueue.Count > 0 ? _criminalQueue.First.Value : null;

        #endregion

        // ──────────────────────────────────────────────
        #region Unity Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            // Lightweight singleton setup.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _queueDirection = _queueDirection.normalized;
        }

        private void Start()
        {
            InitializeQueue();
        }

        #endregion

        // ──────────────────────────────────────────────
        #region Public Methods
        // ──────────────────────────────────────────────

        /// <summary>
        /// Spawns the initial batch of criminals defined by <see cref="_initialQueueSize"/>
        /// and positions each one along the queue line starting at <see cref="_queueStartPoint"/>.
        /// Should be called once during scene initialization.
        /// </summary>
        public void InitializeQueue()
        {
            // TODO: Loop _initialQueueSize times.
            //       For each iteration, call SpawnCriminalAtBack().
            //       Assign a random handcuff requirement to each criminal
            //       (between _minHandcuffRequirement and _maxHandcuffRequirement).
            //       Activate the speech-bubble UI on the front criminal.
        }

        /// <summary>
        /// Attempts to feed the given <paramref name="amount"/> of handcuffs to the
        /// front criminal. The criminal absorbs only as many as it still needs.
        /// If the requirement is met as a result, <see cref="OnFrontCriminalSatisfied"/> is called.
        /// </summary>
        /// <param name="amount">Number of handcuffs available on the output desk.</param>
        /// <returns>
        /// The number of handcuffs actually consumed (0 if none were needed or queue is empty).
        /// </returns>
        public int TryFeedHandcuff(int amount)
        {
            // TODO: Guard — return 0 if queue is empty or amount <= 0.
            // TODO: Determine how many the front criminal still needs.
            // TODO: Calculate consumed = Mathf.Min(amount, remaining).
            // TODO: Apply consumed handcuffs to the front criminal's current count.
            // TODO: Update the speech-bubble UI (Current / Required).
            // TODO: If requirement is now met, call OnFrontCriminalSatisfied().
            // TODO: Return consumed.
            return 0;
        }

        /// <summary>
        /// Handles the full sequence when the front criminal's handcuff requirement is met:
        /// <list type="number">
        ///   <item>Fires <see cref="OnCriminalSatisfied"/>.</item>
        ///   <item>Triggers the criminal's transformation into a prisoner uniform.</item>
        ///   <item>Commands the criminal to walk to <see cref="_jailTargetPoint"/>.</item>
        ///   <item>Removes the criminal from the queue via <see cref="RemoveFrontCriminal"/>.</item>
        ///   <item>Advances the remaining queue via <see cref="AdvanceQueue"/>.</item>
        ///   <item>Spawns a replacement criminal at the back via <see cref="SpawnCriminalAtBack"/>.</item>
        /// </list>
        /// </summary>
        public void OnFrontCriminalSatisfied()
        {
            // TODO: Cache reference to the current front criminal.
            // TODO: Fire OnCriminalSatisfied event.
            // TODO: Tell the criminal to change into prisoner uniform (visual swap).
            // TODO: Tell the criminal to walk towards _jailTargetPoint.
            // TODO: Call RemoveFrontCriminal().
            // TODO: Call AdvanceQueue().
            // TODO: Call SpawnCriminalAtBack().
            // TODO: Activate speech-bubble UI on the new front criminal.
        }

        /// <summary>
        /// Moves every criminal currently in the queue forward by one slot toward the desk.
        /// Each criminal animates (lerp / tween) from its current position to the position
        /// one index lower. Fires <see cref="OnQueueAdvanced"/> when the shift is initiated.
        /// </summary>
        public void AdvanceQueue()
        {
            // TODO: Fire OnQueueAdvanced event.
            // TODO: Iterate through _criminalQueue with index tracking.
            //       For each criminal, compute target position via GetQueuePosition(index).
            //       Animate the criminal to that position (DOTween / coroutine / lerp).
        }

        /// <summary>
        /// Spawns (or retrieves from an object pool) a new criminal at the back of the queue.
        /// Assigns a random handcuff requirement and positions it at the correct world position.
        /// Fires <see cref="OnNewCriminalSpawned"/>.
        /// </summary>
        /// <returns>The newly created <see cref="CriminalController"/>.</returns>
        public CriminalController SpawnCriminalAtBack()
        {
            // TODO: Instantiate or pool-get _criminalPrefab.
            // TODO: Get / cache the CriminalController component.
            // TODO: Assign a random handcuff requirement (min..max inclusive).
            // TODO: Calculate back index = _criminalQueue.Count.
            // TODO: Position the criminal at GetQueuePosition(backIndex).
            // TODO: Add the criminal to the back of _criminalQueue.
            // TODO: Fire OnNewCriminalSpawned event.
            // TODO: Return the CriminalController.
            return null;
        }

        /// <summary>
        /// Removes and returns the criminal at the front of the queue.
        /// Does NOT destroy the GameObject — the caller (or the criminal itself)
        /// is responsible for walking to jail and eventual recycling / pooling.
        /// </summary>
        /// <returns>The <see cref="CriminalController"/> that was removed, or <c>null</c> if empty.</returns>
        public CriminalController RemoveFrontCriminal()
        {
            // TODO: Guard — return null if queue is empty.
            // TODO: Cache _criminalQueue.First.Value.
            // TODO: Remove the first node from the linked list.
            // TODO: Return the cached reference.
            return null;
        }

        #endregion

        // ──────────────────────────────────────────────
        #region Helper Methods
        // ──────────────────────────────────────────────

        /// <summary>
        /// Calculates the world position for a given queue index.
        /// Index 0 is the front of the queue (at the desk).
        /// <c>position = _queueStartPoint.position + _queueDirection * (_queueSpacing * index)</c>
        /// </summary>
        /// <param name="index">Zero-based index in the queue (0 = front / desk).</param>
        /// <returns>World-space position for that queue slot.</returns>
        public Vector3 GetQueuePosition(int index)
        {
            // TODO: Return _queueStartPoint.position + _queueDirection * (_queueSpacing * index).
            return Vector3.zero;
        }

        #endregion
    }
}
