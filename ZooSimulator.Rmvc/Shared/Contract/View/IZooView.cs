using RMVC;

namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>Presentation of the animal enclosure and each animal's current condition.</summary>
    public interface IZooView : IRContract
    {
        /// <summary>Displays immutable animal state using stable identifiers.</summary>
        /// <remarks>
        /// The view owns scene layout, animation, local selection and health-bar components.
        /// Calls can arrive on worker threads; dispatch to the presentation context and
        /// ignore revisions older than the last displayed revision there. Health rules
        /// remain in the application; render frames never advance simulation time.
        /// </remarks>
        void ShowZoo(ZooSnapshotDto snapshot);
    }
}
