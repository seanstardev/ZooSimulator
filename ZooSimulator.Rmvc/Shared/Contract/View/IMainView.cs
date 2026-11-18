using System;
using RMVC;

namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>Application composition, presentation readiness and fresh-game intent.</summary>
    /// <remarks>
    /// The host owns the view and composes separate zoo, clock and feeding contract
    /// objects. Readiness does not define a future zoo-opening or inventory journey.
    /// </remarks>
    public interface IMainView : IRContract
    {
        /// <summary>Raised after attachment when the initial composed presentation is usable.</summary>
        /// <remarks>Repeated readiness refreshes current state without restarting the clock.</remarks>
        event Action? Ready;

        /// <summary>Requests a fresh game population and a fresh full hour interval.</summary>
        event Action? ResetRequested;
    }
}
