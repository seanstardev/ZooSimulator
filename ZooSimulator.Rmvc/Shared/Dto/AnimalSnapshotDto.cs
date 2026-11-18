namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>An immutable observation of one animal.</summary>
    public sealed class AnimalSnapshotDto
    {
        /// <summary>Stable identifier within this zoo, starting at one.</summary>
        public int Id { get; }

        /// <summary>The animal's species.</summary>
        public AnimalSpeciesEnum Species { get; }

        /// <summary>Unrounded health percentage, between zero and one hundred.</summary>
        public double Health { get; }

        /// <summary>The current survival and walking status.</summary>
        public AnimalStatusEnum Status { get; }

        internal AnimalSnapshotDto(
            int id,
            AnimalSpeciesEnum species,
            double health,
            AnimalStatusEnum status)
        {
            Id = id;
            Species = species;
            Health = health;
            Status = status;
        }
    }
}
