using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Seeds enclosure/time presentation on readiness and starts the clock once.</summary>
    internal sealed class StartupCmd : CommandBase<RVoid>
    {
        protected override IRCommandLock[] GetLocks()
            => [ base.SimulationLock ];

        protected override Task<RVoid> Run()
        {
            var snapshot = base.ZooModel.Snapshot();

            base.ClockProxy.Start();
            base.ZooMediator.ShowZoo(snapshot);
            base.ClockMediator.ShowTime(base.ClockProxy.ObserveTime(
                snapshot.Revision,
                snapshot.HoursElapsed));

            return Complete();
        }
    }
}
