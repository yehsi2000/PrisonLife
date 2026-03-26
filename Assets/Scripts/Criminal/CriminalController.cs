using System;
using UnityEngine;

namespace ArcadeIdle.Core
{
    /// <summary>
    /// Per-criminal MonoBehaviour that lives on each criminal GameObject.
    /// Manages the individual criminal's handcuff requirement state,
    /// visual uniform swap, speech-bubble UI binding, and walk-to-jail behaviour.
    /// </summary>
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
        [Tooltip("The speech-bubble UI component showing Current / Required handcuffs.")]
        [SerializeField] private GameObject _speechBubble;

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

        #region Public Methods

        /// <summary>
        /// Initializes the criminal with a random handcuff requirement.
        /// Called by <see cref="CriminalQueueManager"/> when spawning.
        /// </summary>
        /// <param name="requiredAmount">Number of handcuffs this criminal demands.</param>
        public void Setup(int requiredAmount)
        {
            // TODO: Set _requiredHandcuffs = requiredAmount.
            // TODO: Reset _currentHandcuffs to 0.
            // TODO: Update speech-bubble UI text.
            // TODO: Ensure criminal outfit is active, prisoner uniform is inactive.
        }

        /// <summary>
        /// Feeds handcuffs to this criminal. Returns how many were actually consumed.
        /// </summary>
        /// <param name="amount">Number of handcuffs offered.</param>
        /// <returns>Number actually consumed (capped at <see cref="Remaining"/>).</returns>
        public int ReceiveHandcuffs(int amount)
        {
            // TODO: Calculate consumed = Mathf.Min(amount, Remaining).
            // TODO: Add consumed to _currentHandcuffs.
            // TODO: Update speech-bubble UI.
            // TODO: If IsSatisfied, invoke OnRequirementMet.
            // TODO: Return consumed.
            return 0;
        }

        /// <summary>
        /// Triggers the visual transformation from criminal clothes to prisoner uniform.
        /// </summary>
        public void TransformToPrisoner()
        {
            // TODO: Deactivate _criminalOutfit.
            // TODO: Activate _prisonerUniform.
            // TODO: Hide _speechBubble.
            // TODO: Play transformation VFX/SFX if desired.
        }

        /// <summary>
        /// Commands this criminal to walk toward the jail cell target position.
        /// </summary>
        /// <param name="jailPosition">World-space position of the jail cell.</param>
        public void WalkToJail(Vector3 jailPosition)
        {
            // TODO: Set NavMeshAgent destination to jailPosition.
            // TODO: Play walk animation.
            // TODO: On arrival, notify ObjectPoolManager to despawn this criminal.
        }

        /// <summary>
        /// Shows or hides the speech-bubble UI above this criminal.
        /// </summary>
        /// <param name="visible"><c>true</c> to show, <c>false</c> to hide.</param>
        public void SetSpeechBubbleVisible(bool visible)
        {
            // TODO: Set _speechBubble.SetActive(visible).
        }

        /// <summary>
        /// Resets this criminal for object-pool reuse.
        /// </summary>
        public void ResetForPool()
        {
            // TODO: Reset _requiredHandcuffs and _currentHandcuffs to 0.
            // TODO: Restore criminal outfit, hide prisoner uniform.
            // TODO: Hide speech bubble.
            // TODO: Reset NavMeshAgent state.
        }

        #endregion
    }
}
