using System;
using System.Collections.Generic;
using System.Linq;
using RMVC;
using ZooSimulator.Rmvc.Model.Zoo;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Owns zoo time and animals; serializes complete mutations and snapshot reads.</summary>
    internal sealed class ZooModel : RModel, IZooModel
    {
        private readonly object gate = new object();
        private readonly List<Animal> animals = new List<Animal>();
        private long hoursElapsed;
        private long revision;
        private long generation;

        /// <summary>Creates five animals per species at full health, exactly once.</summary>
        public void Initialise()
        {
            lock (gate)
            {
                if (animals.Count != 0)
                {
                    throw new InvalidOperationException(
                        "The zoo has already been initialised.");
                }

                Populate();
            }
        }

        /// <summary>Returns a stable copy of the complete current state.</summary>
        public ZooSnapshotDto Snapshot()
        {
            lock (gate)
            {
                RequireInitialised();

                return CreateSnapshot();
            }
        }

        /// <summary>Applies one full hour using one percentage per animal, including dead animals.</summary>
        /// <remarks>All input identities are validated before time or any animal changes.</remarks>
        public void AdvanceHour(HourDecayDto decay)
        {
            ZooSnapshotDto snapshot;

            lock (gate)
            {
                RequireInitialised();

                if (decay.Animals.Count != animals.Count)
                {
                    throw new ArgumentException(
                        "An hour requires one decay entry for every animal.",
                        nameof(decay));
                }

                for (int index = 0; index < animals.Count; index++)
                {
                    if (decay.Animals[index].AnimalId != animals[index].Id)
                    {
                        throw new ArgumentException(
                            "Decay identities must match the zoo's stable animal order.",
                            nameof(decay));
                    }
                }

                hoursElapsed++;

                for (int index = 0; index < animals.Count; index++)
                    animals[index].AdvanceHour(
                        decay.Animals[index].Percentage, hoursElapsed);

                revision++;

                snapshot = CreateSnapshot();
            }

            base.Execute(new ZooChangedCmd(snapshot));
        }

        /// <summary>Feeds all living animals using the percentage shared by their species.</summary>
        /// <remarks>Full-health or all-dead no-op feedings do not publish a state change.</remarks>
        public void Feed(FeedingDto feeding)
        {
            ZooSnapshotDto snapshot;

            lock (gate)
            {
                RequireInitialised();

                bool changed = false;

                foreach (var animal in animals)
                    changed |= animal.Feed(feeding.PercentageFor(animal.Species), hoursElapsed);

                if (changed == false)
                    return;

                revision++;

                snapshot = CreateSnapshot();
            }

            base.Execute(new ZooChangedCmd(snapshot));
        }

        /// <summary>Applies a targeted increase atomically without changing zoo time or other animals.</summary>
        public void FeedAnimal(AnimalFeedingDto feeding)
        {
            ZooSnapshotDto snapshot;

            lock (gate)
            {
                RequireInitialised();

                var animal = animals.SingleOrDefault(item => item.Id == feeding.AnimalId)
                    ?? throw new ArgumentException(
                        "The target animal does not exist.",
                        nameof(feeding));

                if (animal.Feed(feeding.Percentage, hoursElapsed) == false)
                    return;

                revision++;

                snapshot = CreateSnapshot();
            }

            base.Execute(new ZooChangedCmd(snapshot));
        }

        /// <summary>Recreates the opening population, advancing revision/generation to reject stale display data.</summary>
        public void Reset()
        {
            ZooSnapshotDto snapshot;

            lock (gate)
            {
                RequireInitialised();
                animals.Clear();
                Populate();
                hoursElapsed = 0;
                generation++;
                revision++;
                snapshot = CreateSnapshot();
            }

            base.Execute(new ZooChangedCmd(snapshot));
        }

        private void Populate()
        {
            for (int index = 0; index < 5; index++)
            {
                animals.Add(new Monkey(animals.Count + 1));
                animals.Add(new Giraffe(animals.Count + 1));
                animals.Add(new Elephant(animals.Count + 1));
            }
        }

        private ZooSnapshotDto CreateSnapshot()
            => new ZooSnapshotDto(
                revision,
                hoursElapsed,
                animals.Select(animal => animal.Snapshot()),
                generation);

        private void RequireInitialised()
        {
            if (animals.Count == 0)
            {
                throw new InvalidOperationException(
                    "The zoo has not been initialised.");
            }
        }
    }
}
