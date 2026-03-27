using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrisonLife.Core
{
    /// <summary>
    /// Per-criminal MonoBehaviour that lives on each criminal GameObject.
    /// Manages the individual criminal's handcuff requirement state,
    /// visual uniform swap, speech-bubble UI binding, and walk-to-jail behaviour.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CriminalController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Requirement State")]
        [Tooltip("Total number of handcuffs this criminal requires.")]
        [SerializeField] private int _requiredHandcuffs;

        [Tooltip("Number of handcuffs this criminal has received so far.")]
        [SerializeField] private int _currentHandcuffs;

        [Header("Visual References")]
        [Tooltip("Root GameObject for the criminal outfit (deactivated when transforming).")]
        [SerializeField] private GameObject _criminalOutfit;

        [Tooltip("Root GameObject for the prisoner uniform (activated on transformation).")]
        [SerializeField] private GameObject _prisonerUniform;

        [Header("UI")]
        [Tooltip("The shared speech-bubble background shown for both handcuff count and no-cell notice.")]
        [SerializeField] private GameObject _speechBubble;

        [Tooltip("TextMeshPro text inside the handcuff speech bubble (e.g. '2/5'). Child of _speechBubble.")]
        [SerializeField] private TextMeshProUGUI _handcuffText;

        [Tooltip("Image used as a bottom-to-top green fill indicator inside the speech bubble.")]
        [SerializeField] private Image _speechBubbleFill;

        [Tooltip("Speed (fill amount per second) at which the green fill animates.")]
        [SerializeField] private float _fillSpeed = 2f;

        [Tooltip("'No Cell!' text/object inside the shared speech bubble. Shown instead of handcuff text when jail is full.")]
        [SerializeField] private TextMeshProUGUI _noCellText;

        [Header("Movement")]
        [Tooltip("World-space units per second the criminal walks toward the jail.")]
        [SerializeField] private float _moveSpeed = 3f;

        #endregion

        #region Events

        /// <summary>Fires when this criminal's handcuff requirement is fully met.</summary>
        public event Action<CriminalController> OnRequirementMet;

        #endregion

        #region Public Properties

        /// <summary>Total handcuffs needed by this criminal.</summary>
        public int RequiredHandcuffs => _requiredHandcuffs;

        /// <summary>Handcuffs received so far.</summary>
        public int CurrentHandcuffs => _currentHandcuffs;

        /// <summary>How many more handcuffs this criminal still needs.</summary>
        public int Remaining => _requiredHandcuffs - _currentHandcuffs;

        /// <summary><c>true</c> when <see cref="CurrentHandcuffs"/> >= <see cref="RequiredHandcuffs"/>.</summary>
        public bool IsSatisfied => _currentHandcuffs >= _requiredHandcuffs;

        #endregion

        #region Private Fields

        private Rigidbody _rigidbody;
        private bool _isWalking;
        private Coroutine _fillCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Freeze rotation so criminals don't tip over when colliding.
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // Speech bubble must be hidden until the criminal is at the desk.
            if (_speechBubble != null) _speechBubble.SetActive(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the criminal with a random handcuff requirement.
        /// Called by <see cref="CriminalQueueManager"/> when spawning.
        /// </summary>
        /// <param name="requiredAmount">Number of handcuffs this criminal demands.</param>
        public void Setup(int requiredAmount)
        {
            _requiredHandcuffs = requiredAmount;
            _currentHandcuffs = 0;

            // Ensure correct outfit visibility.
            if (_criminalOutfit != null) _criminalOutfit.SetActive(true);
            if (_prisonerUniform != null) _prisonerUniform.SetActive(false);

            // Refresh bubble text silently — do NOT show the bubble yet.
            // The bubble is only shown when this criminal reaches the desk.
            UpdateSpeechBubbleTextSilent();

            // Hide the shared speech bubble and the no-cell content.
            if (_speechBubble != null) _speechBubble.SetActive(false);
        }

        /// <summary>
        /// Feeds handcuffs to this criminal. Returns how many were actually consumed.
        /// </summary>
        /// <param name="amount">Number of handcuffs offered.</param>
        /// <returns>Number actually consumed (capped at <see cref="Remaining"/>).</returns>
        public int ReceiveHandcuffs(int amount)
        {
            int consumed = Mathf.Min(amount, Remaining);
            _currentHandcuffs += consumed;

            UpdateSpeechBubbleTextSilent();

            if (IsSatisfied)
            {
                OnRequirementMet?.Invoke(this);
            }

            return consumed;
        }

        /// <summary>
        /// Triggers the visual transformation from criminal clothes to prisoner uniform.
        /// </summary>
        public void TransformToPrisoner()
        {
            if (_criminalOutfit != null) _criminalOutfit.SetActive(false);
            if (_prisonerUniform != null) _prisonerUniform.SetActive(true);

            // Hide handcuff-count speech bubble once the criminal becomes a prisoner.
            SetSpeechBubbleVisible(false);
        }

        /// <summary>
        /// Commands this criminal to walk toward the jail in two steps:
        /// first to the <paramref name="cornerPoint"/>, then (if the jail is not full)
        /// straight into <paramref name="cellPosition"/>.
        /// </summary>
        /// <param name="cornerPoint">Waypoint at the entrance of the jail corridor.</param>
        /// <param name="cellPosition">World-space position of the target cell.</param>
        public void WalkToJail(Vector3 cornerPoint, Vector3 cellPosition)
        {
            if (_isWalking) return;
            _isWalking = true;
            StartCoroutine(WalkToJailRoutine(cornerPoint, cellPosition));
        }

        /// <summary>
        /// Shows or hides the shared speech-bubble background.
        /// </summary>
        /// <param name="visible"><c>true</c> to show, <c>false</c> to hide.</param>
        public void SetSpeechBubbleVisible(bool visible)
        {
            if (_speechBubble != null) _speechBubble.SetActive(visible);
        }

        /// <summary>
        /// Shows the shared speech bubble with the handcuff-count content.
        /// Call this when the criminal arrives at the desk and is ready to receive handcuffs.
        /// </summary>
        public void ShowHandcuffBubble()
        {
            // Show the shared speech bubble background.
            if (_speechBubble != null) _speechBubble.SetActive(true);

            // Show handcuff count elements, hide no-cell notice.
            if (_handcuffText != null) _handcuffText.gameObject.SetActive(true);
            if (_speechBubbleFill != null) _speechBubbleFill.gameObject.SetActive(true);
            if (_noCellText != null) _noCellText.gameObject.SetActive(false);

            UpdateSpeechBubbleTextSilent();
        }

        /// <summary>
        /// Reuses the shared speech bubble to display the "No Cell!" notice.
        /// Hides the handcuff-count elements; shows only the no-cell text.
        /// </summary>
        public void ShowNoCellBubble()
        {
            // Show the shared speech bubble background.
            if (_speechBubble != null) _speechBubble.SetActive(true);

            // Hide handcuff count elements; show no-cell notice.
            if (_handcuffText != null) _handcuffText.gameObject.SetActive(false);
            if (_speechBubbleFill != null) _speechBubbleFill.gameObject.SetActive(false);
            if (_noCellText != null) _noCellText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Resets this criminal for object-pool reuse.
        /// </summary>
        public void ResetForPool()
        {
            // Stop any in-progress movement or animations.
            StopAllCoroutines();
            _isWalking = false;
            _fillCoroutine = null;

            // Reset counters.
            _requiredHandcuffs = 0;
            _currentHandcuffs = 0;

            // Reset visuals.
            if (_criminalOutfit != null) _criminalOutfit.SetActive(true);
            if (_prisonerUniform != null) _prisonerUniform.SetActive(false);

            // Reset fill image.
            if (_speechBubbleFill != null) _speechBubbleFill.fillAmount = 0f;

            // Hide all speech bubbles.
            SetSpeechBubbleVisible(false);

            // Reset physics velocity.
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine that drives the two-step walk-to-jail sequence.
        /// Step 1 — walk to the corner of the jail corridor.
        /// Step 2 — if a cell is available, walk straight into the cell and despawn;
        ///          otherwise stop and (if first to detect) show the "No Cell!" bubble.
        /// </summary>
        private IEnumerator WalkToJailRoutine(Vector3 cornerPoint, Vector3 cellPosition)
        {
            // ── Step 1: Walk to the corner waypoint ──────────────────────────────
            yield return MoveToPosition(cornerPoint);

            // ── Jail-full check at the corner point ──────────────────────────────
            CriminalQueueManager queue = CriminalQueueManager.Instance;

            if (queue != null && queue.IsJailFull)
            {
                // Only the first criminal to detect the full jail shows the notice.
                if (!queue.JailFullNoticeShown)
                {
                    queue.JailFullNoticeShown = true;
                    ShowNoCellBubble();
                }

                _isWalking = false;
                yield break; // Stay at corner; do not enter the cell area.
            }

            // Reserve a cell slot before entering.
            if (queue != null) queue.OccupyJailCell();

            // ── Step 2: Walk straight into the cell area ─────────────────────────
            yield return MoveToPosition(cellPosition);

            // Criminal stays inside the jail indefinitely — no despawn.
            _isWalking = false;
        }

        /// <summary>
        /// Coroutine that smoothly moves this transform toward <paramref name="target"/>
        /// along the XZ plane at <see cref="_moveSpeed"/> units per second.
        /// </summary>
        private IEnumerator MoveToPosition(Vector3 target)
        {
            // Keep the criminal's Y constant (avoid sinking into the ground on slopes).
            target.y = transform.position.y;

            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    _moveSpeed * Time.deltaTime);

                // Face the direction of travel.
                Vector3 direction = target - transform.position;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(direction);

                yield return null;
            }

            transform.position = target;
        }

        /// <summary>
        /// Refreshes the handcuff count text and fill, without changing bubble visibility.
        /// Called during <see cref="Setup"/> (silent, before the criminal reaches the desk)
        /// and from <see cref="ReceiveHandcuffs"/> (bubble is already visible at that point).
        /// </summary>
        private void UpdateSpeechBubbleTextSilent()
        {
            if (_handcuffText != null)
                _handcuffText.text = $"{_requiredHandcuffs - _currentHandcuffs}";

            // Animate the fill image toward the new ratio.
            if (_speechBubbleFill != null)
            {
                float targetFill = _requiredHandcuffs > 0
                    ? (float)_currentHandcuffs / _requiredHandcuffs
                    : 0f;

                if (_fillCoroutine != null) StopCoroutine(_fillCoroutine);
                _fillCoroutine = StartCoroutine(AnimateFill(targetFill));
            }
        }

        /// <summary>
        /// Smoothly animates <see cref="_speechBubbleFill"/> from its current fill amount
        /// to <paramref name="targetFill"/> (bottom-to-top, green).
        /// </summary>
        private IEnumerator AnimateFill(float targetFill)
        {
            // Ensure fill type is set correctly at runtime.
            _speechBubbleFill.fillMethod = Image.FillMethod.Vertical;
            _speechBubbleFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            _speechBubbleFill.color = Color.green;

            while (!Mathf.Approximately(_speechBubbleFill.fillAmount, targetFill))
            {
                _speechBubbleFill.fillAmount = Mathf.MoveTowards(
                    _speechBubbleFill.fillAmount,
                    targetFill,
                    _fillSpeed * Time.deltaTime);
                yield return null;
            }

            _speechBubbleFill.fillAmount = targetFill;
            _fillCoroutine = null;
        }

        #endregion
    }
}
