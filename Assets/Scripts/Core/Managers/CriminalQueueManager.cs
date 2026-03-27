using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrisonLife.Core
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
        [Tooltip("Waypoint at the corner / entrance of the jail corridor. " +
                 "Criminals walk here first, then check capacity before entering a cell.")]
        [SerializeField] private Transform _jailCornerPoint;

        [Tooltip("World position inside the cell area where satisfied criminals walk to " +
                 "after passing the corner check.")]
        [SerializeField] private Transform _jailTargetPoint;

        [Tooltip("Maximum number of prisoners the jail can hold simultaneously.")]
        [SerializeField] private int _maxJailCapacity = 10;

        [Header("Queue Movement")]
        [Tooltip("Seconds it takes for each criminal to slide to its new queue position.")]
        [SerializeField] private float _queueShiftDuration = 0.3f;

        #endregion

        // ──────────────────────────────────────────────
        #region Private Fields
        // ──────────────────────────────────────────────

        /// <summary>
        /// Ordered queue of active criminals. LinkedList is used for O(1)
        /// removal at the front and insertion at the back.
        /// </summary>
        private LinkedList<CriminalController> _criminalQueue = new LinkedList<CriminalController>();

        /// <summary>Current number of prisoners occupying a jail cell.</summary>
        private int _currentJailOccupancy;

        /// <summary>
        /// <c>true</c> once the first criminal to detect a full jail has displayed
        /// the "No Cell!" notice, preventing duplicate notices.
        /// </summary>
        private bool _jailFullNoticeShown;

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

        /// <summary><c>true</c> when the jail has reached its maximum capacity.</summary>
        public bool IsJailFull => _currentJailOccupancy >= _maxJailCapacity;

        /// <summary>
        /// Gets or sets whether the "No Cell!" speech bubble has already been displayed
        /// by the first criminal to detect a full jail. Reset this when a cell opens up.
        /// </summary>
        public bool JailFullNoticeShown
        {
            get => _jailFullNoticeShown;
            set => _jailFullNoticeShown = value;
        }

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
            for (int i = 0; i < _initialQueueSize; i++)
            {
                int requirement = UnityEngine.Random.Range(_minHandcuffRequirement, _maxHandcuffRequirement + 1);
                CriminalController criminal = SpawnCriminalAtBack();
                if (criminal != null)
                    criminal.Setup(requirement);
            }

            // Show the handcuff bubble only for the front criminal at the desk.
            if (FrontCriminal != null)
                FrontCriminal.ShowHandcuffBubble();
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
            if (_criminalQueue.Count == 0 || amount <= 0)
                return 0;

            CriminalController front = FrontCriminal;
            int consumed = front.ReceiveHandcuffs(amount);

            if (front.IsSatisfied)
                OnFrontCriminalSatisfied();

            return consumed;
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
            CriminalController satisfied = FrontCriminal;
            if (satisfied == null) return;

            OnCriminalSatisfied?.Invoke(satisfied);

            // Visual transformation.
            satisfied.TransformToPrisoner();

            // Two-step walk: corner waypoint → cell position.
            Vector3 corner = _jailCornerPoint != null
                ? _jailCornerPoint.position
                : (_jailTargetPoint != null ? _jailTargetPoint.position : Vector3.zero);

            Vector3 cell = _jailTargetPoint != null
                ? _jailTargetPoint.position
                : Vector3.zero;

            satisfied.WalkToJail(corner, cell);

            // Remove from queue and refill.
            RemoveFrontCriminal();
            AdvanceQueue();

            CriminalController newCriminal = SpawnCriminalAtBack();
            if (newCriminal != null)
            {
                int requirement = UnityEngine.Random.Range(_minHandcuffRequirement, _maxHandcuffRequirement + 1);
                newCriminal.Setup(requirement);
            }

            // Show handcuff bubble on the criminal that just stepped up to the desk.
            if (FrontCriminal != null)
                FrontCriminal.ShowHandcuffBubble();
        }

        /// <summary>
        /// Moves every criminal currently in the queue forward by one slot toward the desk.
        /// Each criminal moves from its current position to the position one index lower.
        /// Fires <see cref="OnQueueAdvanced"/> when the shift is initiated.
        /// </summary>
        public void AdvanceQueue()
        {
            OnQueueAdvanced?.Invoke();

            int index = 0;
            foreach (CriminalController criminal in _criminalQueue)
            {
                Vector3 targetPos = GetQueuePosition(index);
                StartCoroutine(SlideToPosition(criminal, targetPos));
                index++;
            }
        }

        /// <summary>
        /// Instantiates a new criminal at the back of the queue.
        /// Positions it at the correct world position.
        /// Fires <see cref="OnNewCriminalSpawned"/>.
        /// </summary>
        /// <returns>The newly created <see cref="CriminalController"/>.</returns>
        public CriminalController SpawnCriminalAtBack()
        {
            GameObject criminalGO = _criminalPrefab != null
                ? Instantiate(_criminalPrefab)
                : null;

            if (criminalGO == null)
            {
                Debug.LogWarning("[CriminalQueueManager] Failed to spawn criminal.");
                return null;
            }

            CriminalController controller = criminalGO.GetComponent<CriminalController>();
            if (controller == null)
            {
                Debug.LogError("[CriminalQueueManager] Criminal prefab is missing CriminalController.");
                return null;
            }

            // Position the criminal at the back of the line.
            int backIndex = _criminalQueue.Count;
            criminalGO.transform.position = GetQueuePosition(backIndex);
            criminalGO.SetActive(true);

            _criminalQueue.AddLast(controller);

            OnNewCriminalSpawned?.Invoke(controller);

            return controller;
        }

        /// <summary>
        /// Removes and returns the criminal at the front of the queue.
        /// Does NOT destroy the GameObject — the criminal itself despawns after reaching a cell.
        /// </summary>
        /// <returns>The <see cref="CriminalController"/> that was removed, or <c>null</c> if empty.</returns>
        public CriminalController RemoveFrontCriminal()
        {
            if (_criminalQueue.Count == 0) return null;

            CriminalController front = _criminalQueue.First.Value;
            _criminalQueue.RemoveFirst();
            return front;
        }

        /// <summary>
        /// Increments the jail occupancy counter by one.
        /// Called by <see cref="CriminalController"/> just before it steps into the cell.
        /// </summary>
        public void OccupyJailCell()
        {
            _currentJailOccupancy++;

            // If a cell opened up previously and now refills, keep the notice flag accurate.
            // (No auto-reset here — designer can call ReleaseJailCell to free a slot.)
        }

        /// <summary>
        /// Decrements the jail occupancy counter by one and clears the full-notice flag
        /// so the next criminal that checks will be able to show "No Cell!" again if needed.
        /// Call this when a prisoner is released or reassigned.
        /// </summary>
        public void ReleaseJailCell()
        {
            _currentJailOccupancy = Mathf.Max(0, _currentJailOccupancy - 1);

            // Allow the next criminal waiting at the corner to display the notice if the
            // jail fills up again.
            if (!IsJailFull)
                _jailFullNoticeShown = false;
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
            if (_queueStartPoint == null) return Vector3.zero;
            return _queueStartPoint.position + _queueDirection * (_queueSpacing * index);
        }

        /// <summary>
        /// Coroutine that linearly slides <paramref name="criminal"/> from its current
        /// position to <paramref name="target"/> over <see cref="_queueShiftDuration"/> seconds.
        /// </summary>
        private IEnumerator SlideToPosition(CriminalController criminal, Vector3 target)
        {
            if (criminal == null) yield break;

            Transform t = criminal.transform;
            Vector3 start = t.position;
            float elapsed = 0f;

            while (elapsed < _queueShiftDuration)
            {
                elapsed += Time.deltaTime;
                t.position = Vector3.Lerp(start, target, elapsed / _queueShiftDuration);
                yield return null;
            }

            t.position = target;
        }

        #endregion
    }
}
