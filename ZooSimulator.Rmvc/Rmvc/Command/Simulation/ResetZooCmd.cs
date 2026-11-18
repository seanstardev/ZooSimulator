using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Starts a fresh population and full hour interval without recreating the application.</summary>
    internal sealed class ResetZooCmd : CommandBase<RVoid>
    {
        protected override IRCommandLock[] GetLocks()
            => [ base.SimulationLock ];

        protected override Task<RVoid> Run()
        {
            base.ClockProxy.Restart();
            base.ZooModel.Reset();

            return Complete();
        }
    }
}
