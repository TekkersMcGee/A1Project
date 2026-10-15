using UnityEngine;

/// <summary>
/// Controls a first-person character using a Rigidbody for movement and jumping.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyCharacterController : MonoBehaviour
{
    /// <summary>
    /// Settings that control the character's movement.
    /// </summary>
    [Header("Movement")]

    /// <summary>
    /// The character's maximum horizontal movement speed.
    /// </summary>
    [Tooltip("Maximum horizontal movement speed.")]
    [SerializeField] private float moveSpeed = 5.0f;

    /// <summary>
    /// The height the character should reach when jumping, in world units.
    /// </summary>
    [Tooltip("Height the character should reach when jumping, in world units.")]
    [SerializeField] private float jumpHeight = 2.0f;

    /// <summary>
    /// Settings used to detect whether the character is touching the ground.
    /// </summary>
    [Header("Ground Check")]

    /// <summary>
    /// A child Transform positioned just below the bottom of the player's collider.
    /// </summary>
    [Tooltip("Child Transform positioned just below the bottom of the player's collider.")]
    [SerializeField] private Transform groundCheck;

    /// <summary>
    /// The radius of the sphere used to check for nearby ground colliders.
    /// </summary>
    [Tooltip("Radius of the sphere used to check whether the player is touching the ground.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    /// <summary>
    /// The layers that count as ground, such as floors and platforms.
    /// Exclude the player's own layer.
    /// </summary>
    [Tooltip("Layers that count as ground, such as floors and platforms. Exclude the player's layer.")]
    [SerializeField] private LayerMask groundMask;

    /// <summary>
    /// The Rigidbody that controls the character's physics movement.
    /// </summary>
    private Rigidbody rb;

    /// <summary>
    /// The horizontal and vertical movement input read during Update.
    /// </summary>
    private Vector2 moveInput;

    /// <summary>
    /// Whether the player pressed the jump button since the last physics update.
    /// </summary>
    private bool jumpRequested;

    /// <summary>
    /// Whether the ground check currently detects the ground.
    /// </summary>
    private bool isGrounded;

    /// <summary>
    /// Gets the Rigidbody and prevents physics collisions from tipping the character over.
    /// </summary>
    private void Awake()
    {
        // Cache the Rigidbody so we do not need to search for it repeatedly.
        rb = GetComponent<Rigidbody>();

        // Add rotation constraints while preserving any other constraints
        // already set on the Rigidbody.
        rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>
    /// Reads player input once per rendered frame.
    /// </summary>
    private void Update()
    {
        // Input.GetAxis returns a value between -1 and 1 for each direction.
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        // Record the jump press here because it might happen between physics
        // updates. HandleJump uses this request in FixedUpdate.
        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }
    }

    /// <summary>
    /// Checks the ground and applies movement and jumping during a physics update.
    /// </summary>
    private void FixedUpdate()
    {
        // Check the ground before deciding whether a jump can happen.
        HandleGroundedState();

        // Change the player's horizontal velocity for this physics step.
        MoveCharacter();

        // Apply the jump after movement, while preserving horizontal velocity.
        HandleJump();
    }

    /// <summary>
    /// Checks whether the ground-check sphere overlaps a collider on the ground layers.
    /// </summary>
    private void HandleGroundedState()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    /// <summary>
    /// Sets the character's horizontal velocity based on movement input.
    /// </summary>
    private void MoveCharacter()
    {
        // Convert the input into a direction relative to the character's facing direction.
        Vector3 moveDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        // Prevent diagonal input from moving faster than forward input.
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Keep the current vertical velocity so gravity and jumping continue to work.
        Vector3 velocity = rb.linearVelocity;

        // Set the horizontal velocity. Unity handles the vertical movement separately.
        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;

        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Applies an upward velocity when the character requests a jump while grounded.
    /// </summary>
    private void HandleJump()
    {
        if (jumpRequested && isGrounded)
        {
            // Calculate the upward speed needed to reach jumpHeight,
            // using the project's gravity setting.
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            rb.linearVelocity = velocity;
        }

        // Clear the request after this physics step. A jump pressed while
        // airborne is ignored rather than buffered until the character lands.
        jumpRequested = false;
    }
}