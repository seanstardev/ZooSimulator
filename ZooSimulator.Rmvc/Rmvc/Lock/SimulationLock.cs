using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Coordinates mutations and clock restart across a game reset.</summary>
    internal sealed class SimulationLock : IRCommandLock { }
}
