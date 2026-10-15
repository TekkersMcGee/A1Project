using UnityEngine;

namespace ClassroomAI
{
    /// <summary>
    /// Moves the enemy between its assigned patrol points, pausing in Idle at each point.
    /// </summary>
    public class PatrolState : EnemyState
    {
        /// <summary>Gets the name shown for this state in the enemy's debug display.</summary>
        public override string Name => "Patrol";

        /// <summary>The next time the agent should recalculate its path.</summary>
        private float nextRepath;

        /// <summary>Creates the patrol state for the supplied enemy.</summary>
        /// <param name="owner">The enemy whose components and settings this state uses.</param>
        public PatrolState(EnemyAI owner) : base(owner) { }

        /// <summary>Allows an immediate destination update after entering the state.</summary>
        public override void Enter()
        {
            nextRepath = 0f;
        }

        /// <summary>Moves toward the active patrol point and reacts to visibility or arrival.</summary>
        public override void Tick()
        {
            // A visible target interrupts the patrol and starts chasing or attacking.
            if (enemy.TryVisibleTransition())
            {
                return;
            }

            enemy.agent.speed = enemy.patrolSpeed;
            enemy.agent.stoppingDistance = 0.15f;

            // If there are no usable patrol points, return to Idle.
            if (!enemy.HasPatrolPoint())
            {
                enemy.ChangeState(enemy.Idle);
                return;
            }

            // Recalculate the route periodically instead of every frame.
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + enemy.repathSeconds;

                // If the patrol point cannot be reached, skip it and pause in Idle.
                if (!enemy.MoveTo(enemy.PatrolPosition))
                {
                    enemy.AdvancePatrol();
                    enemy.ChangeState(enemy.Idle);
                    return;
                }
            }

            // When the agent reaches its destination, advance the point and pause before moving again.
            bool closeToPoint = Vector3.Distance(enemy.transform.position, enemy.PatrolPosition) <= 0.3f;
            bool agentHasArrived = !enemy.agent.pathPending && enemy.agent.hasPath &&
                                   enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + 0.1f;

            if (!enemy.agent.pathPending && (closeToPoint || agentHasArrived))
            {
                enemy.AdvancePatrol();
                enemy.ChangeState(enemy.Idle);
            }
        }
    }
}
