using UnityEngine;

namespace ClassroomAI
{
    /// <summary>
    /// Moves the enemy toward the visible target or the target's last known position.
    /// </summary>
    public class ChaseState : EnemyState
    {
        /// <summary>Gets the name shown for this state in the enemy's debug display.</summary>
        public override string Name => "Chase";

        /// <summary>The next time the agent should update its destination.</summary>
        private float nextRepath;

        /// <summary>Creates the chase state for the supplied enemy.</summary>
        /// <param name="owner">The enemy whose components and settings this state uses.</param>
        public ChaseState(EnemyAI owner) : base(owner) { }

        /// <summary>Allows an immediate destination update after entering the state.</summary>
        public override void Enter()
        {
            nextRepath = 0f;
        }

        /// <summary>Transitions to Attack or Idle when appropriate and otherwise follows the target.</summary>
        public override void Tick()
        {
            // Attack immediately when the target is visible and within range.
            if (enemy.CanSeeTarget && enemy.TargetDistance <= enemy.attackRange)
            {
                enemy.ChangeState(enemy.Attack);
                return;
            }

            // Stop searching when the enemy can no longer see or remember the target.
            if (!enemy.CanSeeTarget && !enemy.RemembersTarget)
            {
                enemy.ChangeState(enemy.Idle);
                return;
            }

            enemy.agent.speed = enemy.chaseSpeed;
            enemy.agent.stoppingDistance = 0.15f;

            // Recalculate the path at a controlled interval rather than every frame.
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + enemy.repathSeconds;

                // When the target is out of sight, lastSeen is its last visible position.
                if (!enemy.MoveTo(enemy.lastSeen))
                {
                    enemy.StopMoving();
                }
            }
        }
    }
}
