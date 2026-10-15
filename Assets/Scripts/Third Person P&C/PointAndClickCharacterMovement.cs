using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves a NavMeshAgent to the position clicked by the player.
/// </summary>
public class PointAndClickCharacterMovement : MonoBehaviour
{
    /// <summary>
    /// References used to detect clicks and move the character.
    /// </summary>
    [Header("References")]

    /// <summary>
    /// The camera used to convert the mouse position into a world-space ray.
    /// </summary>
    [Tooltip("The camera used to cast a ray from the mouse position into the scene.")]
    [SerializeField] private Camera cam;

    /// <summary>
    /// The NavMeshAgent that moves the character to the clicked position.
    /// </summary>
    [Tooltip("The NavMeshAgent that moves the character to the clicked position.")]
    [SerializeField] private NavMeshAgent agent;

    /// <summary>
    /// Checks for a left mouse-button click each frame.
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetDestinationFromMouseClick();
        }
    }

    /// <summary>
    /// Casts a ray from the camera through the mouse position and,
    /// if it hits a collider, sets that position as the agent's destination.
    /// </summary>
    private void SetDestinationFromMouseClick()
    {
        // Create a ray starting at the camera and passing through the mouse cursor.
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Check whether the ray hits a collider in the scene.
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Ask the NavMeshAgent to move to the point that was hit.
            agent.SetDestination(hit.point);
        }
    }
}
