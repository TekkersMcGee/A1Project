using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ClassroomAI
{
    /// <summary>
    /// Provides simple first-person movement and mouse-look using a CharacterController.
    /// Supports the Input System package and the legacy Input Manager.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class StarterPlayer : MonoBehaviour
    {
        /// <summary>The camera or view Transform that pitches up and down.</summary>
        [Header("References")]
        [Tooltip("The camera or view Transform that rotates vertically with mouse movement.")]
        public Transform view;

        /// <summary>The character's horizontal movement speed.</summary>
        [Header("Movement")]
        [Tooltip("The character's horizontal movement speed in world units per second.")]
        [Min(0f)]
        public float moveSpeed = 5f;

        /// <summary>The multiplier applied to mouse movement when looking around.</summary>
        [Header("Mouse Look")]
        [Tooltip("Mouse movement multiplier. Increase this value for faster looking.")]
        [Min(0f)]
        public float mouseSensitivity = 0.12f;

        /// <summary>The CharacterController that moves the player and handles collisions.</summary>
        private CharacterController controller;

        /// <summary>The camera's vertical angle and the player's current vertical movement speed.</summary>
        private float pitch, verticalSpeed;

        /// <summary>Finds the required CharacterController component.</summary>
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        /// <summary>Locks and hides the cursor when the scene starts.</summary>
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Releases and shows the cursor when this component is disabled.</summary>
        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Reads player input, updates the view rotation, and moves the character.</summary>
        private void Update()
        {
            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool escape = false;
            bool click = false;

#if ENABLE_INPUT_SYSTEM
            // Read WASD and Escape when the Input System package is active.
            if (Keyboard.current != null)
            {
                move.x = (Keyboard.current.dKey.isPressed ? 1 : 0) -
                         (Keyboard.current.aKey.isPressed ? 1 : 0);
                move.y = (Keyboard.current.wKey.isPressed ? 1 : 0) -
                         (Keyboard.current.sKey.isPressed ? 1 : 0);
                escape = Keyboard.current.escapeKey.wasPressedThisFrame;
            }

            // Read mouse movement and left-click input when a mouse is available.
            if (Mouse.current != null)
            {
                look = Mouse.current.delta.ReadValue();
                click = Mouse.current.leftButton.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            // Read the same controls through the legacy Input Manager.
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 15f;
            escape = Input.GetKeyDown(KeyCode.Escape);
            click = Input.GetMouseButtonDown(0);
#endif

            // Escape releases the cursor; clicking the game view locks it again.
            if (escape)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (click)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Do not allow a defeated player or a player who finished the run to move.
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health && (health.CurrentHealth <= 0 || health.HasFinished))
            {
                return;
            }

            // Apply horizontal body rotation and vertical view rotation only while the cursor is locked.
            if (Cursor.lockState == CursorLockMode.Locked && view)
            {
                transform.Rotate(0f, look.x * mouseSensitivity, 0f);

                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -80f, 80f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            // Keep a small downward speed while grounded so the controller stays in contact with slopes.
            if (controller.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }

            // Apply the project's gravity to vertical movement.
            verticalSpeed += Physics.gravity.y * Time.deltaTime;

            // Clamp diagonal input so it does not move faster than straight input.
            move = Vector2.ClampMagnitude(move, 1f);

            // Convert local movement input into a world-space direction.
            Vector3 motion = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
            motion.y = verticalSpeed;

            // CharacterController.Move expects a displacement, so multiply velocity by frame time.
            controller.Move(motion * Time.deltaTime);
        }
    }
}
