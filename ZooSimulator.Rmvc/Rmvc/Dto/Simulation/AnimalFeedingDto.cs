using System;

namespace ZooSimulator.Rmvc
{
    /// <summary>A validated targeted feeding request, an explicit gameplay extension.</summary>
    internal sealed class AnimalFeedingDto
    {
        public int AnimalId { get; }
        public int Percentage { get; }

        public AnimalFeedingDto(int animalId, int percentage)
        {
            if (animalId <= 0)
                throw new ArgumentOutOfRangeException(nameof(animalId));

            if (percentage < 10 || percentage > 25)
                throw new ArgumentOutOfRangeException(nameof(percentage));

            AnimalId = animalId;
            Percentage = percentage;
        }
    }
}
