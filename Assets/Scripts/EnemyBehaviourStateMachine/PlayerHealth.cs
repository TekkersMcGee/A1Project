using UnityEngine;

namespace ClassroomAI
{
    /// <summary>
    /// Stores the player's health, accepts damage, and displays health and defeat messages.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        /// <summary>The player's starting and maximum health.</summary>
        [Tooltip("The player's starting health and maximum health value.")]
        [Min(1)]
        public int maxHealth = 100;

        /// <summary>Gets the player's current health.</summary>
        public int CurrentHealth { get; private set; }

        /// <summary>Gets whether the player has reached the finish.</summary>
        public bool HasFinished { get; private set; }

        /// <summary>Marks the player's run as complete so further damage is ignored.</summary>
        public void FinishRun()
        {
            HasFinished = true;
        }

        /// <summary>Initializes current health from the configured maximum health.</summary>
        private void Awake()
        {
            // Ensure the player starts with at least one health point.
            CurrentHealth = Mathf.Max(1, maxHealth);
        }

        /// <summary>Reduces current health by a non-negative amount unless the run is complete.</summary>
        /// <param name="amount">The amount of damage to apply.</param>
        public void TakeDamage(int amount)
        {
            if (!HasFinished)
            {
                // Clamp both the damage and resulting health so neither goes below zero.
                CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
            }
        }

        /// <summary>Displays the health bar text and, when appropriate, the defeat message.</summary>
        private void OnGUI()
        {
            GUI.Box(
                new Rect(12f, 12f, 360f, 65f),
                "Health: " + CurrentHealth + " / " + maxHealth +
                "\nWASD move | mouse look | Esc release mouse"
            );

            if (CurrentHealth == 0)
            {
                GUI.Box(new Rect(12f, 82f, 360f, 45f), "Defeated. Stop and restart Play Mode.");
            }
        }
    }
}
