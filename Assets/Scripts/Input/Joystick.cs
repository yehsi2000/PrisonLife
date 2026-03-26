using UnityEngine;
using UnityEngine.EventSystems;

namespace ArcadeIdle.Input
{
    /// <summary>
    /// Base class for UI-based virtual joysticks.
    /// Implements drag handling via Unity's EventSystem interfaces.
    /// Attach to a UI Image that acts as the joystick background.
    /// </summary>
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        #region Serialized Fields

        [Header("Joystick Settings")]
        [Tooltip("Maximum distance the handle can move from the center, in pixels.")]
        [SerializeField] private float _handleRange = 1f;

        [Tooltip("Input values below this threshold are treated as zero (dead zone).")]
        [SerializeField] private float _deadZone = 0f;

        [Header("Component References")]
        [Tooltip("RectTransform of the joystick background (outer circle).")]
        [SerializeField] protected RectTransform _background;

        [Tooltip("RectTransform of the joystick handle (inner circle / knob).")]
        [SerializeField] private RectTransform _handle;

        #endregion

        #region Private Fields

        private RectTransform _baseRect;
        private Canvas _canvas;
        private Camera _canvasCamera;
        private Vector2 _input = Vector2.zero;

        #endregion

        #region Public Properties

        /// <summary>Horizontal input axis value in the range [-1, 1].</summary>
        public float Horizontal => _input.x;

        /// <summary>Vertical input axis value in the range [-1, 1].</summary>
        public float Vertical => _input.y;

        /// <summary>Combined input direction as a Vector2 (magnitude clamped to 1).</summary>
        public Vector2 Direction => new Vector2(Horizontal, Vertical);

        /// <summary>Maximum pixel distance the handle travels from center.</summary>
        protected float HandleRange { get => _handleRange; set => _handleRange = Mathf.Abs(value); }

        /// <summary>Dead zone threshold below which input is zeroed.</summary>
        protected float DeadZone { get => _deadZone; set => _deadZone = Mathf.Abs(value); }

        /// <summary>Reference to the handle's RectTransform.</summary>
        protected RectTransform Handle => _handle;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Caches required references on initialization.
        /// </summary>
        protected virtual void Start()
        {
            _baseRect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            if (_canvas != null)
            {
                _canvasCamera = (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    ? _canvas.worldCamera
                    : null;
            }

            // Center the handle on start
            Vector2 center = new Vector2(0.5f, 0.5f);
            _background.pivot = center;
            _handle.anchorMin = center;
            _handle.anchorMax = center;
            _handle.pivot = center;
            _handle.anchoredPosition = Vector2.zero;
        }

        #endregion

        #region Event System Handlers

        /// <summary>
        /// Called when the player first touches/clicks the joystick area.
        /// </summary>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        /// <summary>
        /// Called every frame while the player drags within the joystick area.
        /// Calculates the normalized input direction from the touch position.
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, _canvasCamera, out position);

            // Normalize position relative to background size
            Vector2 sizeDelta = _background.sizeDelta;
            position = new Vector2(
                position.x / (sizeDelta.x * 0.5f),
                position.y / (sizeDelta.y * 0.5f));

            // Clamp to unit circle
            _input = (position.magnitude > 1f) ? position.normalized : position;

            // Apply dead zone
            if (_input.magnitude < _deadZone)
            {
                _input = Vector2.zero;
            }
            else
            {
                // Rescale input so that the edge of the dead zone maps to 0
                _input = _input.normalized * ((_input.magnitude - _deadZone) / (1f - _deadZone));
            }

            // Move the handle visual
            _handle.anchoredPosition = _input * (sizeDelta.x * 0.5f) * _handleRange;
        }

        /// <summary>
        /// Called when the player lifts their finger / releases the mouse.
        /// Resets the joystick input and handle position to center.
        /// </summary>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
        }

        #endregion

        #region Protected Helpers

        /// <summary>
        /// Converts a screen-space position into a position within this joystick's
        /// base RectTransform. Used by subclasses to reposition the joystick origin.
        /// </summary>
        /// <param name="screenPosition">Screen-space touch/click position.</param>
        /// <returns>Local point within the base RectTransform.</returns>
        protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _baseRect, screenPosition, _canvasCamera, out localPoint);
            return localPoint;
        }

        #endregion
    }
}
