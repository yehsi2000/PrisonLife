using UnityEngine;
using UnityEngine.EventSystems;

namespace PrisonLife.Input
{
    /// <summary>
    /// A floating joystick that appears at the player's touch position and
    /// disappears when released. Ideal for mobile hyper-casual games where
    /// the joystick should not occupy permanent screen space.
    /// <para>Inherits core drag logic from <see cref="Joystick"/>.</para>
    /// </summary>
    public class FloatingJoystick : Joystick
    {
        #region Unity Lifecycle

        /// <summary>
        /// Hides the joystick background on start since the floating joystick
        /// only appears when the player touches the screen.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _background.gameObject.SetActive(false);
        }

        #endregion

        #region Event System Overrides

        /// <summary>
        /// Shows the joystick at the touch position and begins tracking input.
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            _background.gameObject.SetActive(true);
            _background.position = eventData.position;
            base.OnPointerDown(eventData);
        }

        /// <summary>
        /// Hides the joystick and resets input when the player lifts their finger.
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            _background.gameObject.SetActive(false);
            base.OnPointerUp(eventData);
        }

        #endregion
    }
}
