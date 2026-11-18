using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>One validated percentage increase per species for a single feeding.</summary>
    internal sealed class FeedingDto
    {
        private readonly int monkey;
        private readonly int giraffe;
        private readonly int elephant;

        public FeedingDto(int monkey, int giraffe, int elephant)
        {
            Validate(monkey, nameof(monkey));
            Validate(giraffe, nameof(giraffe));
            Validate(elephant, nameof(elephant));

            this.monkey = monkey;
            this.giraffe = giraffe;
            this.elephant = elephant;
        }

        public int PercentageFor(AnimalSpeciesEnum species)
            => species switch
            {
                AnimalSpeciesEnum.Monkey => monkey,
                AnimalSpeciesEnum.Giraffe => giraffe,
                AnimalSpeciesEnum.Elephant => elephant,
                _ => throw new ArgumentOutOfRangeException(nameof(species))
            };

        private static void Validate(int percentage, string parameterName)
        {
            if (percentage < 10 || percentage > 25)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
