using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Model.Zoo
{
    /// <summary>An elephant below 70% cannot walk and has until the next hour to recover.</summary>
    internal sealed class Elephant : Animal
    {
        private long? recoveryDeadline;

        public Elephant(int id) : base(id, AnimalSpeciesEnum.Elephant) { }

        protected override void BeforeDecay(long hour)
        {
            // Resolve the previous hour's recovery window before this hour's new decay.
            if (recoveryDeadline.HasValue && hour >= recoveryDeadline.Value)
                base.Status = AnimalStatusEnum.Dead;
        }

        protected override void EvaluateHealth(long hour)
        {
            if (base.Health >= 70)
            {
                base.Status = AnimalStatusEnum.Alive;
                recoveryDeadline = null;

                return;
            }

            base.Status = AnimalStatusEnum.UnableToWalk;

            if (recoveryDeadline.HasValue == false)
                recoveryDeadline = hour + 1;
        }
    }
}
