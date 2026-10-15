using UnityEngine;

namespace ClassroomAI
{
    /// <summary>
    /// Stops the enemy, faces the visible target, and attacks when in range and off cooldown.
    /// </summary>
    public class AttackState : EnemyState
    {
        /// <summary>Gets the name shown for this state in the enemy's debug display.</summary>
        public override string Name => "Attack";

        /// <summary>Creates the attack state for the supplied enemy.</summary>
        /// <param name="owner">The enemy whose components and settings this state uses.</param>
        public AttackState(EnemyAI owner) : base(owner) { }

        /// <summary>Stops the NavMeshAgent when the enemy enters the attack state.</summary>
        public override void Enter()
        {
            enemy.StopMoving();
        }

        /// <summary>Checks target visibility and range, turns toward the target, and attacks when ready.</summary>
        public override void Tick()
        {
            // If the enemy cannot currently see the target, either keep chasing
            // its remembered position or return to idle when its memory has expired.
            if (!enemy.CanSeeTarget)
            {
                enemy.ChangeState(enemy.RemembersTarget ? (EnemyState)enemy.Chase : enemy.Idle);
                return;
            }

            // Move back to Chase once the target leaves the attack range plus a
            // small padding value. The padding prevents rapid state switching.
            if (enemy.TargetDistance > enemy.attackRange + enemy.exitAttackPadding)
            {
                enemy.ChangeState(enemy.Chase);
                return;
            }

            enemy.StopMoving();

            // Rotate toward the target on the horizontal plane only.
            Vector3 facing = enemy.target.position - enemy.transform.position;
            facing.y = 0f;

            if (facing.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facing);
                enemy.transform.rotation = Quaternion.RotateTowards(
                    enemy.transform.rotation,
                    targetRotation,
                    360f * Time.deltaTime
                );
            }

            // Keep the cooldown on EnemyAI so leaving and re-entering this state
            // does not reset the time before the next attack.
            if (enemy.TargetDistance <= enemy.attackRange && Time.time >= enemy.nextAttack)
            {
                enemy.nextAttack = Time.time + enemy.attackCooldown;
                enemy.PerformAttack();
            }
        }
    }
}
