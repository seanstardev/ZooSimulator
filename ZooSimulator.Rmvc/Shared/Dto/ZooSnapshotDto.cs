using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>An owned immutable snapshot of zoo time and every animal.</summary>
    public sealed class ZooSnapshotDto
    {
        /// <summary>Monotonically increasing state revision; views must reject older deliveries.</summary>
        public long Revision { get; }

        /// <summary>Game generation; increments on reset while Revision continues increasing.</summary>
        public long Generation { get; }

        /// <summary>Completed simulated hours since opening; the initial value is zero.</summary>
        public long HoursElapsed { get; }

        /// <summary>Immutable animals in stable identifier order, backed by a private copy.</summary>
        public IReadOnlyList<AnimalSnapshotDto> Animals { get; }

        internal ZooSnapshotDto(
            long revision,
            long hoursElapsed,
            IEnumerable<AnimalSnapshotDto> animals,
            long generation = 0)
        {
            Revision = revision;
            Generation = generation;
            HoursElapsed = hoursElapsed;
            Animals = new ReadOnlyCollection<AnimalSnapshotDto>(animals.ToArray());
        }
    }
}
