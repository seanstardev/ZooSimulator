using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Atomic operations on the authoritative zoo state.</summary>
    internal interface IZooModel : IRModel
    {
        /// <summary>Creates the initial fifteen animals once, without starting real time.</summary>
        void Initialise();

        /// <summary>Recreates a healthy opening population at hour zero with a new generation.</summary>
        void Reset();

        /// <summary>Copies the complete state under the model's state gate.</summary>
        ZooSnapshotDto Snapshot();

        /// <summary>Validates and applies every animal's decay, then publishes one new revision.</summary>
        void AdvanceHour(HourDecayDto decay);

        /// <summary>Applies species-shared increases and publishes only meaningful changes.</summary>
        void Feed(FeedingDto feeding);

        /// <summary>Feeds one animal only; leaves all other animals unchanged.</summary>
        void FeedAnimal(AnimalFeedingDto feeding);
    }
}
