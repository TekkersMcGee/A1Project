using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ClassroomAI
{
    /// <summary>
    /// Coordinates the enemy's NavMesh movement, target detection, state machine, and attacks.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        /// <summary>Scene objects and layers used by the enemy.</summary>
        [Header("References")]

        /// <summary>The Transform the enemy should detect, chase, and attack.</summary>
        [Tooltip("The player Transform that this enemy should detect and pursue.")]
        public Transform target;

        /// <summary>The Transform used as the origin of the enemy's line-of-sight checks.</summary>
        [Tooltip("Optional eye position for sight checks. If empty, a point above the enemy is used.")]
        public Transform eyes;

        /// <summary>Ordered positions that the enemy visits while patrolling.</summary>
        [Tooltip("Patrol destinations in order. Assign walkable positions on the baked NavMesh.")]
        public Transform[] patrolPoints;

        /// <summary>Layers that can block the enemy's view of the target.</summary>
        [Tooltip("Layers treated as walls or cover during sight checks. Exclude the enemy and player layers.")]
        public LayerMask obstacleMask;

        /// <summary>Settings that determine whether the enemy can see its target.</summary>
        [Header("Sight")]

        /// <summary>The maximum distance at which the enemy can see the target.</summary>
        [Tooltip("Maximum distance at which the enemy can see the target, in world units.")]
        [Min(0.1f)]
        public float sightRange = 12f;

        /// <summary>The full horizontal angle of the enemy's field of view.</summary>
        [Tooltip("The full horizontal field-of-view angle, in degrees.")]
        [Range(1f, 360f)]
        public float fieldOfView = 110f;

        /// <summary>How long, in seconds, the enemy remembers a target after losing sight of it.</summary>
        [Tooltip("How many seconds the enemy remembers the target's last seen position.")]
        [Min(0f)]
        public float memorySeconds = 3f;

        /// <summary>The vertical offset used when aiming line-of-sight checks at the target.</summary>
        [Tooltip("Vertical offset from the target's position used by the line-of-sight check.")]
        public float targetAimHeight = 1f;

        /// <summary>Settings that control patrolling, chasing, and idle waits.</summary>
        [Header("Movement")]

        /// <summary>The NavMeshAgent speed while patrolling.</summary>
        [Tooltip("NavMeshAgent movement speed while patrolling.")]
        [Min(0f)]
        public float patrolSpeed = 2f;

        /// <summary>The NavMeshAgent speed while chasing the target.</summary>
        [Tooltip("NavMeshAgent movement speed while chasing the target.")]
        [Min(0f)]
        public float chaseSpeed = 4f;

        /// <summary>How long, in seconds, the enemy waits while idle.</summary>
        [Tooltip("How many seconds the enemy waits before beginning or resuming its patrol.")]
        [Min(0f)]
        public float idleSeconds = 2f;

        /// <summary>How often, in seconds, the enemy recalculates its movement path.</summary>
        [Tooltip("Minimum time between NavMesh destination updates.")]
        [Min(0.05f)]
        public float repathSeconds = 0.2f;

        /// <summary>Settings that control attack range, timing, and damage.</summary>
        [Header("Attack")]

        /// <summary>The distance at which the enemy can attack the target.</summary>
        [Tooltip("Maximum distance at which the enemy can deal damage.")]
        [Min(0.1f)]
        public float attackRange = 1.8f;

        /// <summary>Extra distance allowed before the enemy leaves Attack and returns to Chase.</summary>
        [Tooltip("Extra distance beyond attack range before the enemy switches back to Chase. Helps prevent rapid state switching.")]
        [Min(0f)]
        public float exitAttackPadding = 0.4f;

        /// <summary>Time, in seconds, between attacks.</summary>
        [Tooltip("Time in seconds that must pass between attacks.")]
        [Min(0.1f)]
        public float attackCooldown = 1f;

        /// <summary>Amount of health removed from the player by each attack.</summary>
        [Tooltip("Health points removed from the player by each attack.")]
        [Min(0)]
        public int damage = 10;

        /// <summary>UnityEvent invoked whenever the enemy attacks.</summary>
        [Tooltip("Optional UnityEvent invoked each time the enemy attacks. Connect additional effects in the Inspector.")]
        public UnityEvent onAttack = new UnityEvent();

        /// <summary>Options for displaying the enemy's current state and debug information.</summary>
        [Header("Debug")]

        /// <summary>Whether to show attack messages and the current-state label.</summary>
        [Tooltip("When enabled, show attack messages in the Console and the current state above the enemy.")]
        public bool showDebug = true;

        /// <summary>The current state's display name, shown for debugging in the Inspector.</summary>
        [Tooltip("Read-only debug display of the enemy's current state.")]
        [SerializeField] private string currentStateName = "Idle";

        /// <summary>Gets the state that is currently active.</summary>
        public EnemyState CurrentState { get; private set; }

        /// <summary>Gets this enemy's idle state instance.</summary>
        public IdleState Idle { get; private set; }

        /// <summary>Gets this enemy's patrol state instance.</summary>
        public PatrolState Patrol { get; private set; }

        /// <summary>Gets this enemy's chase state instance.</summary>
        public ChaseState Chase { get; private set; }

        /// <summary>Gets this enemy's attack state instance.</summary>
        public AttackState Attack { get; private set; }

        /// <summary>Gets whether the target is currently visible.</summary>
        public bool CanSeeTarget { get; private set; }

        /// <summary>The NavMeshAgent used by this enemy's states.</summary>
        internal NavMeshAgent agent;

        /// <summary>The most recent world position where the target was visible.</summary>
        internal Vector3 lastSeen;

        /// <summary>The time at which the enemy may next attack.</summary>
        internal float nextAttack;

        /// <summary>The index of the current destination in patrolPoints.</summary>
        private int patrolIndex;

        /// <summary>Prevents repeating the warning while the agent is off the NavMesh.</summary>
        private bool warned;

        /// <summary>Reusable object for calculating complete NavMesh paths.</summary>
        private NavMeshPath route;

        /// <summary>Gets the eye position, using the assigned eyes Transform or a fallback above the enemy.</summary>
        private Vector3 EyePosition => eyes ? eyes.position : transform.position + Vector3.up * 1.5f;

        /// <summary>Gets whether the NavMeshAgent is enabled and placed on a NavMesh.</summary>
        private bool Ready => agent && agent.enabled && agent.isOnNavMesh;

        /// <summary>Gets whether the target is still within the enemy's memory period.</summary>
        internal bool RemembersTarget =>
            target && target.gameObject.activeInHierarchy && Time.time - lastSeenTime <= memorySeconds;

        /// <summary>Gets the distance between the enemy and target, or infinity if there is no target.</summary>
        internal float TargetDistance =>
            target ? Vector3.Distance(transform.position, target.position) : float.PositiveInfinity;

        /// <summary>The time the enemy last saw the target.</summary>
        private float lastSeenTime = float.NegativeInfinity;

        /// <summary>Finds the NavMeshAgent, creates this enemy's state instances, and prepares a reusable path.</summary>
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            // Create the path here rather than in a field initializer: Unity APIs
            // that initialize native NavMesh data cannot run in a MonoBehaviour constructor.
            route = new NavMeshPath();

            // Each enemy owns its own state objects. These are ordinary C# objects,
            // not separate GameObjects or MonoBehaviour components.
            Idle = new IdleState(this);
            Patrol = new PatrolState(this);
            Chase = new ChaseState(this);
            Attack = new AttackState(this);
        }

        /// <summary>Resets target memory and starts the enemy in its idle state.</summary>
        private void OnEnable()
        {
            lastSeenTime = float.NegativeInfinity;
            ChangeState(Idle);
        }

        /// <summary>Exits the active state and stops the agent when this component is disabled.</summary>
        private void OnDisable()
        {
            CurrentState?.Exit();
            CurrentState = null;

            if (Ready)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        /// <summary>Checks visibility and updates the active state once per frame.</summary>
        private void Update()
        {
            // NavMeshAgent movement methods require the agent to be on a baked NavMesh.
            if (!Ready)
            {
                if (!warned)
                {
                    Debug.LogWarning(name + ": place the agent on a baked NavMesh.", this);
                    warned = true;
                }

                return;
            }

            warned = false;

            // The target must exist and be active before a sight check is attempted.
            CanSeeTarget = target && target.gameObject.activeInHierarchy && InSight();

            if (CanSeeTarget)
            {
                lastSeen = target.position;
                lastSeenTime = Time.time;
            }

            // Only the current state receives a Tick call.
            CurrentState?.Tick();
        }

        /// <summary>Exits the current state and enters another state owned by this enemy.</summary>
        /// <param name="next">The state to activate. Null, foreign, or already-active states are ignored.</param>
        public void ChangeState(EnemyState next)
        {
            // Reject invalid transitions and avoid exiting and re-entering the same state instance.
            if (next == null || next.Owner != this || ReferenceEquals(CurrentState, next))
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = next;
            currentStateName = next.Name;
            CurrentState.Enter();
        }

        /// <summary>Changes to Chase or Attack if the target is currently visible.</summary>
        /// <returns>True if a visible-target transition was made; otherwise false.</returns>
        internal bool TryVisibleTransition()
        {
            if (!CanSeeTarget)
            {
                return false;
            }

            ChangeState(TargetDistance <= attackRange ? (EnemyState)Attack : Chase);
            return true;
        }

        /// <summary>Stops the NavMeshAgent and optionally clears its current path.</summary>
        /// <param name="clearPath">Whether to discard the current path as well as stop movement.</param>
        internal void StopMoving(bool clearPath = false)
        {
            if (!Ready)
            {
                return;
            }

            agent.isStopped = true;

            if (clearPath)
            {
                agent.ResetPath();
            }
        }

        /// <summary>Checks whether at least one assigned patrol point is valid and selects one.</summary>
        /// <returns>True if a non-null patrol point is available; otherwise false.</returns>
        internal bool HasPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return false;
            }

            patrolIndex %= patrolPoints.Length;

            // Skip any unassigned entries and wrap through the array if needed.
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[patrolIndex])
                {
                    return true;
                }

                AdvancePatrol();
            }

            return false;
        }

        /// <summary>Advances the patrol index, wrapping back to the start of the array.</summary>
        internal void AdvancePatrol()
        {
            int pointCount = patrolPoints == null ? 0 : patrolPoints.Length;
            patrolIndex = (patrolIndex + 1) % Mathf.Max(1, pointCount);
        }

        /// <summary>Attempts to set a complete NavMesh path to a world position.</summary>
        /// <param name="position">The destination position in world space.</param>
        /// <returns>True if a complete path was found and assigned; otherwise false.</returns>
        internal bool MoveTo(Vector3 position)
        {
            // Find a nearby point on the NavMesh first. This keeps the destination
            // on walkable ground even if the requested point is slightly off it.
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, agent.areaMask))
            {
                return false;
            }

            // Require a complete route so the enemy does not keep trying to reach
            // a destination separated by an unwalkable gap.
            if (!agent.CalculatePath(hit.position, route) || route.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            agent.isStopped = false;
            return agent.SetPath(route);
        }

        /// <summary>Gets the world position of the currently selected patrol point.</summary>
        internal Vector3 PatrolPosition => patrolPoints[patrolIndex].position;

        /// <summary>Starts the enemy's attack behaviour.</summary>
        internal void PerformAttack()
        {
            DealDamage();
        }

        /// <summary>Checks range, field of view, and obstacles to determine whether the target is visible.</summary>
        /// <returns>True when the target is in range and view and no obstacle blocks the sight line.</returns>
        private bool InSight()
        {
            Vector3 aim = target.position + Vector3.up * targetAimHeight;
            Vector3 delta = aim - EyePosition;

            if (delta.magnitude > sightRange)
            {
                return false;
            }

            // Use a horizontal direction for the field-of-view angle so target height
            // does not change how far left or right the target appears.
            Vector3 horizontal = delta;
            horizontal.y = 0f;

            if (horizontal.sqrMagnitude > 0.001f &&
                Vector3.Angle(transform.forward, horizontal) > fieldOfView * 0.5f)
            {
                return false;
            }

            // The obstacle mask should include cover, but exclude the enemy and player.
            return !Physics.Linecast(EyePosition, aim, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Applies damage to a PlayerHealth component and invokes the attack event.</summary>
        /// <remarks>Override this method to add a wind-up, projectile, or another damage system.</remarks>
        protected virtual void DealDamage()
        {
            PlayerHealth health = target.GetComponentInParent<PlayerHealth>();

            if (health)
            {
                health.TakeDamage(damage);
            }

            onAttack.Invoke();

            if (showDebug)
            {
                Debug.Log(name + " attacked for " + damage, this);
            }
        }

        /// <summary>Draws the sight range, field of view, attack range, and visible target in the Scene view.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = EyePosition;
            Gizmos.DrawWireSphere(origin, sightRange);

            // Draw the horizontal field-of-view boundary as a set of line segments.
            Vector3 previousTip = origin;
            for (int i = 0; i <= 24; i++)
            {
                float angle = -fieldOfView / 2f + fieldOfView * i / 24f;
                Vector3 tip = origin + Quaternion.AngleAxis(angle, Vector3.up) * transform.forward * sightRange;

                if (i == 0 || i == 24)
                {
                    Gizmos.DrawLine(origin, tip);
                }

                if (i > 0)
                {
                    Gizmos.DrawLine(previousTip, tip);
                }

                previousTip = tip;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (Application.isPlaying && CanSeeTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, target.position + Vector3.up * targetAimHeight);
            }
        }

#if UNITY_EDITOR
        /// <summary>Displays the current state above the enemy while playing in the Editor.</summary>
        private void OnGUI()
        {
            if (!showDebug || !Camera.main)
            {
                return;
            }

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.3f);

            if (screenPosition.z > 0f)
            {
                GUI.Label(
                    new Rect(screenPosition.x - 70f, Screen.height - screenPosition.y, 200f, 25f),
                    name + ": " + currentStateName
                );
            }
        }
#endif
    }
}
