using UnityEngine;

/// <summary>
/// Rotates the camera vertically and the player body horizontally
/// in response to mouse movement.
/// </summary>
public class MouseLook : MonoBehaviour
{
    /// <summary>
    /// Settings that control mouse-look behaviour.
    /// </summary>
    [Header("Mouse Look")]

    /// <summary>
    /// Multiplier applied to mouse movement to control look speed.
    /// </summary>
    [Tooltip("Controls how quickly the camera and player body rotate in response to mouse movement.")]
    public float mouseSensitivity = 500f;

    /// <summary>
    /// The player's body Transform, which rotates horizontally with the mouse.
    /// </summary>
    [Tooltip("The player's body Transform. It rotates horizontally as the mouse moves left or right.")]
    public Transform playerBody;

    /// <summary>
    /// The camera's current vertical rotation angle, in degrees.
    /// </summary>
    private float xRotation = 0f;

    /// <summary>
    /// Locks and hides the cursor when the scene starts.
    /// </summary>
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Reads mouse movement once per rendered frame and applies it to the camera and player body.
    /// </summary>
    private void Update()
    {
        // Scale mouse movement by sensitivity and frame time so that the
        // look speed is consistent across different frame rates.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        ApplyVerticalLook(mouseY);
        ApplyHorizontalLook(mouseX);
    }

    /// <summary>
    /// Applies vertical mouse movement to this Transform, usually the camera.
    /// </summary>
    /// <param name="mouseY">The frame-adjusted vertical mouse movement.</param>
    private void ApplyVerticalLook(float mouseY)
    {
        // Subtract mouse movement so moving the mouse upward looks upward.
        xRotation -= mouseY;

        // Prevent the camera from rotating beyond straight up or straight down.
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply the vertical rotation to this Transform only.
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    /// <summary>
    /// Applies horizontal mouse movement to the player's body, if one is assigned.
    /// </summary>
    /// <param name="mouseX">The frame-adjusted horizontal mouse movement.</param>
    private void ApplyHorizontalLook(float mouseX)
    {
        if (playerBody != null)
        {
            // Rotate the body around its local up axis.
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}