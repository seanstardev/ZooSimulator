using System;

namespace ZooSimulator.Rmvc
{
    /// <summary>A validated hourly percentage reduction assigned to one animal.</summary>
    internal sealed class AnimalDecayDto
    {
        public int AnimalId { get; }
        public int Percentage { get; }

        public AnimalDecayDto(int animalId, int percentage)
        {
            if (animalId <= 0)
                throw new ArgumentOutOfRangeException(nameof(animalId));

            if (percentage < 0 || percentage > 20)
                throw new ArgumentOutOfRangeException(nameof(percentage));

            AnimalId = animalId;
            Percentage = percentage;
        }
    }
}
