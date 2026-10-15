namespace ClassroomAI
{
    /// <summary>
    /// Base class defining the lifecycle shared by all enemy behaviour states.
    /// Each state is a plain C# object owned by an <see cref="EnemyAI"/> component.
    /// </summary>
    public abstract class EnemyState
    {
        /// <summary>Gets the enemy that owns this state.</summary>
        public EnemyAI Owner { get; }

        /// <summary>Provides derived states with a short alias for their owning enemy.</summary>
        protected EnemyAI enemy => Owner;

        /// <summary>Gets the display name for this state.</summary>
        public abstract string Name { get; }

        /// <summary>Creates a state associated with one enemy.</summary>
        /// <param name="owner">The enemy that will use this state.</param>
        protected EnemyState(EnemyAI owner)
        {
            Owner = owner;
        }

        /// <summary>Called once when this state becomes active. Override if setup is needed.</summary>
        public virtual void Enter() { }

        /// <summary>Called every frame while this state is active. Derived states must implement it.</summary>
        public abstract void Tick();

        /// <summary>Called once when this state stops being active. Override if cleanup is needed.</summary>
        public virtual void Exit() { }
    }
}
