using UnityEngine;

namespace ClassroomAI
{
    /// <summary>
    /// Keeps the enemy still for a short time, then sends it to patrol when a route is available.
    /// </summary>
    public class IdleState : EnemyState
    {
        /// <summary>Gets the name shown for this state in the enemy's debug display.</summary>
        public override string Name => "Idle";

        /// <summary>The time at which the idle wait is complete.</summary>
        private float waitUntil;

        /// <summary>Creates the idle state for the supplied enemy.</summary>
        /// <param name="owner">The enemy whose components and settings this state uses.</param>
        public IdleState(EnemyAI owner) : base(owner) { }

        /// <summary>Starts the idle timer and stops the enemy, clearing its previous path.</summary>
        public override void Enter()
        {
            waitUntil = Time.time + enemy.idleSeconds;
            enemy.StopMoving(true);
        }

        /// <summary>Checks for a visible target, then begins patrol when the wait has ended.</summary>
        public override void Tick()
        {
            // Seeing the target takes priority over waiting or patrolling.
            if (enemy.TryVisibleTransition())
            {
                return;
            }

            enemy.StopMoving();

            // Only begin patrolling after the idle timer finishes and a patrol point exists.
            if (Time.time >= waitUntil && enemy.HasPatrolPoint())
            {
                enemy.ChangeState(enemy.Patrol);
            }
        }
    }
}
