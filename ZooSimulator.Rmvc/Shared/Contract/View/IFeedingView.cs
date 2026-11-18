using System;
using RMVC;

namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>The whole-zoo feeding interaction, independent of inventory mechanics.</summary>
    public interface IFeedingView : IRContract
    {
        /// <summary>Raised once for each request to feed every species in the zoo.</summary>
        /// <remarks>The application generates one percentage per species for this request.</remarks>
        event Action? FeedRequested;

        /// <summary>Requests feeding only the dropped-on animal, a deliberate gameplay extension.</summary>
        event Action<int>? AnimalFeedRequested;
    }
}
