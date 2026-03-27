using System;
using UnityEngine;

namespace PrisonLife.Items.Visual
{
    /// <summary>
    /// Base visual component for items that animate along a bezier arc from a
    /// start position to a (potentially moving) target transform, then fire a
    /// completion event on arrival.
    /// <para>
    /// Used for mined rocks flying to the player's back-stack, deposited rocks
    /// flying to a processor, handcuffs flying from a processor to a desk, etc.
    /// </para>
    /// This class is intentionally <b>not</b> abstract so it can be used directly,
    /// but every behavioural method is <c>virtual</c> for subclass customisation.
    /// </summary>
    public class FlyingItemVisual : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>Total flight time in seconds from start to destination.</summary>
        [Header("Flight Settings")]
        [SerializeField, Tooltip("Total flight time in seconds.")]
        private float _flightDuration = 0.5f;

        /// <summary>
        /// How high (in world-space Y units) the item arcs above the midpoint
        /// between start and target.
        /// </summary>
        [SerializeField, Tooltip("Peak arc height above the midpoint of the flight path.")]
        private float _arcHeight = 2f;

        /// <summary>Whether the item should spin while in flight.</summary>
        [Header("Rotation")]
        [SerializeField, Tooltip("Spin the object while it is in flight.")]
        private bool _rotateWhileFlying = true;

        /// <summary>Rotation speed in degrees per second when <see cref="_rotateWhileFlying"/> is enabled.</summary>
        [SerializeField, Tooltip("Degrees per second of rotation while flying.")]
        private float _rotationSpeed = 720f;

        #endregion

        #region Private Fields

        private Vector3 _startPosition;
        private Transform _targetTransform;
        private float _elapsedTime;
        private bool _isFlying;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the item reaches its destination.
        /// Subscribers receive a reference to this <see cref="FlyingItemVisual"/>
        /// so pooling systems can reclaim the object.
        /// </summary>
        public event Action<FlyingItemVisual> OnFlightComplete;

        #endregion

        #region Public Properties

        /// <summary>
        /// <c>true</c> while the item is actively animating along its flight arc.
        /// </summary>
        public bool IsFlying => _isFlying;

        #endregion

        #region Public Methods

        /// <summary>
        /// Configures the flight parameters and immediately begins the arc animation.
        /// Call this right after spawning or retrieving the item from a pool.
        /// </summary>
        /// <param name="startPos">World-space position where the flight begins.</param>
        /// <param name="target">
        /// The destination transform. The item will track this every frame,
        /// so moving targets (e.g., a walking player) are supported.
        /// </param>
        /// <param name="duration">
        /// Optional override for <see cref="_flightDuration"/>.
        /// Pass <c>-1</c> (or omit) to use the serialized default.
        /// </param>
        public void Initialize(Vector3 startPos, Transform target, float duration = -1f)
        {
            _startPosition = startPos;
            _targetTransform = target;
            if (duration > 0f) _flightDuration = duration;
            _elapsedTime = 0f;
            _isFlying = true;
            transform.position = startPos;
        }

        /// <summary>
        /// Immediately cancels the in-progress flight without firing
        /// <see cref="OnFlightComplete"/>. Useful when returning the object
        /// to a pool or cleaning up on scene transitions.
        /// </summary>
        public void Cancel()
        {
            if (!_isFlying) return;
            ResetState();
        }

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Drives the flight animation every frame while <see cref="_isFlying"/> is <c>true</c>.
        /// </summary>
        protected virtual void Update()
        {
            if (!_isFlying) return;

            // 1. Accumulate elapsed time.
            _elapsedTime += Time.deltaTime;

            // 2. Calculate normalised progress t ∈ [0, 1].
            float t = Mathf.Clamp01(_elapsedTime / _flightDuration);

            // 3. Sample the current target position (supports moving targets).
            Vector3 targetPos = _targetTransform != null ? _targetTransform.position : transform.position;

            // 4. Evaluate the bezier/arc position for the current t.
            transform.position = EvaluateArcPosition(_startPosition, targetPos, t);

            // 5. Optionally rotate the item while in flight.
            if (_rotateWhileFlying)
            {
                transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
            }

            // 6. Check if the flight is complete (t >= 1).
            if (t >= 1f)
            {
                OnArrived();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Evaluates a position along a parabolic (quadratic bezier) arc between
        /// <paramref name="start"/> and <paramref name="end"/> at normalised time
        /// <paramref name="t"/>.
        /// </summary>
        /// <param name="start">World-space start position of the arc.</param>
        /// <param name="end">World-space end position of the arc.</param>
        /// <param name="t">Normalised time in the range [0, 1].</param>
        /// <returns>The interpolated position on the arc.</returns>
        protected virtual Vector3 EvaluateArcPosition(Vector3 start, Vector3 end, float t)
        {
            Vector3 midpoint = (start + end) / 2f;
            Vector3 controlPoint = midpoint + Vector3.up * _arcHeight;

            float u = 1f - t;
            return u * u * start
                 + 2f * u * t * controlPoint
                 + t * t * end;
        }

        /// <summary>
        /// Called when the item reaches its destination (<c>t >= 1</c>).
        /// Fires <see cref="OnFlightComplete"/>, snaps to the target position,
        /// and resets internal state.
        /// <para>
        /// Override in subclasses to spawn arrival VFX, play SFX, or trigger
        /// additional game logic before calling <c>base.OnArrived()</c>.
        /// </para>
        /// </summary>
        protected virtual void OnArrived()
        {
            if (_targetTransform != null)
                transform.position = _targetTransform.position;
            OnFlightComplete?.Invoke(this);
            ResetState();
        }

        /// <summary>
        /// Clears all runtime flight fields back to their defaults so the
        /// component is ready for reuse (e.g., when returned to an object pool).
        /// </summary>
        protected virtual void ResetState()
        {
            _isFlying = false;
            _elapsedTime = 0f;
            _startPosition = Vector3.zero;
            _targetTransform = null;
        }

        #endregion
    }
}
