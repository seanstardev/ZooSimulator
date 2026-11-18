using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Model.Zoo
{
    /// <summary>Zoo-owned animal state and proportional health arithmetic.</summary>
    internal abstract class Animal
    {
        public int Id { get; }
        public AnimalSpeciesEnum Species { get; }
        public double Health { get; private set; } = 100;
        public AnimalStatusEnum Status { get; protected set; }
            = AnimalStatusEnum.Alive;

        protected Animal(int id, AnimalSpeciesEnum species)
        {
            Id = id;
            Species = species;
        }

        /// <summary>Resolves any existing deadline, then removes a percentage of current health.</summary>
        public void AdvanceHour(double percentage, long hour)
        {
            ValidatePercentage(percentage);

            if (Status == AnimalStatusEnum.Dead)
                return;

            BeforeDecay(hour);

            if (Status == AnimalStatusEnum.Dead)
                return;

            Health *= 1 - percentage / 100;
            EvaluateHealth(hour);
        }

        /// <summary>Increases current health proportionally, capped at 100%; never revives the dead.</summary>
        public bool Feed(double percentage, long hour)
        {
            ValidatePercentage(percentage);

            if (Status == AnimalStatusEnum.Dead)
                return false;

            double previousHealth = Health;
            Health = Math.Min(100, Health * (1 + percentage / 100));
            EvaluateHealth(hour);

            return Health != previousHealth;
        }

        public AnimalSnapshotDto Snapshot()
            => new AnimalSnapshotDto(Id, Species, Health, Status);

        protected virtual void BeforeDecay(long hour) { }

        protected abstract void EvaluateHealth(long hour);

        private static void ValidatePercentage(double percentage)
        {
            if (double.IsNaN(percentage) || percentage < 0 || percentage > 100)
                throw new ArgumentOutOfRangeException(nameof(percentage));
        }
    }
}
