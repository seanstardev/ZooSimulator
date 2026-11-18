using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ZooSimulator.Rmvc
{
    /// <summary>Owned decay inputs in the zoo snapshot's stable animal order.</summary>
    internal sealed class HourDecayDto
    {
        public IReadOnlyList<AnimalDecayDto> Animals { get; }

        public HourDecayDto(IEnumerable<AnimalDecayDto> animals)
        {
            if (animals == null)
                throw new ArgumentNullException(nameof(animals));

            var copy = animals.ToArray();

            if (copy.Any(animal => animal == null))
                throw new ArgumentException(
                    "Decay entries cannot be null.",
                    nameof(animals));

            Animals = new ReadOnlyCollection<AnimalDecayDto>(copy);
        }
    }
}
