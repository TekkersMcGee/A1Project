using UnityEngine;

namespace ClassroomAI
{
    /// <summary>
    /// Detects when a living player reaches the finish trigger and displays a completion message.
    /// </summary>
    public class FinishZone : MonoBehaviour
    {
        /// <summary>Whether the player has reached this finish zone.</summary>
        private bool finished;

        /// <summary>Checks entering colliders for a living player.</summary>
        /// <param name="other">The collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            // GetComponentInParent also works when the collider is on a child object.
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            if (health && health.CurrentHealth > 0)
            {
                finished = true;
                health.FinishRun();
                Debug.Log("Finish reached!");
            }
        }

        /// <summary>Draws the completion message after the player reaches the finish.</summary>
        private void OnGUI()
        {
            if (finished)
            {
                GUI.Box(
                    new Rect(Screen.width / 2f - 180f, 100f, 360f, 55f),
                    "Finish reached! Prototype complete.\nStop and restart Play Mode to try again."
                );
            }
        }
    }
}
