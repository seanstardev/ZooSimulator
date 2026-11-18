using System;
using System.Threading.Tasks;
using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Forwards one model-owned immutable state change to the presentation boundary.</summary>
    internal sealed class ZooChangedCmd : CommandBase<RVoid>
    {
        private readonly ZooSnapshotDto snapshot;

        public ZooChangedCmd(ZooSnapshotDto snapshot)
        {
            this.snapshot = snapshot
                ?? throw new ArgumentNullException(nameof(snapshot));
        }

        protected override IRCommandLock[] GetLocks()
            => [ base.SimulationLock ];

        protected override Task<RVoid> Run()
        {
            if (snapshot.Generation != base.ZooModel.Snapshot().Generation)
                return Complete();

            base.ZooMediator.ShowZoo(snapshot);
            base.ClockMediator.ShowTime(base.ClockProxy.ObserveTime(
                snapshot.Revision,
                snapshot.HoursElapsed));

            return Complete();
        }
    }
}
