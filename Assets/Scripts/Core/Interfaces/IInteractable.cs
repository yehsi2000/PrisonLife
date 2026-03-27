using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Core.Interfaces
{
    /// <summary>
    /// Contract for any object the player can stand near to trigger a repeated interaction.
    /// Implementations include resource processors, item desks, and upgrade unlock zones.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Whether the interactable is currently available for interaction.
        /// Returns <c>false</c> when the object is on cooldown, depleted, or
        /// otherwise unable to accept player input.
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// The world-space point the player should face or stand near
        /// in order to interact with this object.
        /// </summary>
        Transform InteractionPoint { get; }

        /// <summary>
        /// Delay in seconds between repeated interaction ticks while
        /// the player remains inside the trigger zone (e.g., one rock
        /// deposited per tick).
        /// </summary>
        float InteractionInterval { get; }

        /// <summary>
        /// Called once when the player first enters the trigger zone
        /// associated with this interactable.
        /// Use this to begin repeating interaction logic, show UI prompts, etc.
        /// </summary>
        /// <param name="player">The <see cref="PlayerController"/> that entered the zone.</param>
        void OnPlayerEnter(PlayerController player);

        /// <summary>
        /// Called once when the player leaves the trigger zone
        /// associated with this interactable.
        /// Use this to stop repeating interaction logic, hide UI prompts, etc.
        /// </summary>
        /// <param name="player">The <see cref="PlayerController"/> that exited the zone.</param>
        void OnPlayerExit(PlayerController player);

        /// <summary>
        /// Called on each interaction tick while the player remains inside
        /// the trigger zone and <see cref="CanInteract"/> is <c>true</c>.
        /// The tick rate is governed by <see cref="InteractionInterval"/>.
        /// </summary>
        /// <param name="player">The <see cref="PlayerController"/> performing the interaction.</param>
        void Interact(PlayerController player);
    }
}
