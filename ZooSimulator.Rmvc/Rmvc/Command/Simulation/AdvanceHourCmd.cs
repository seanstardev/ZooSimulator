using System.Collections.Generic;
using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Obtains a separate decay percentage for every animal and advances one whole hour.</summary>
    internal sealed class AdvanceHourCmd : CommandBase<RVoid>
    {
        private readonly long scheduleVersion;

        public AdvanceHourCmd(long scheduleVersion)
        {
            this.scheduleVersion = scheduleVersion;
        }

        protected override IRCommandLock[] GetLocks()
            => [ base.SimulationLock ];

        protected override Task<RVoid> Run()
        {
            if (base.ClockProxy.IsCurrentSchedule(scheduleVersion) == false)
                return Complete();

            var snapshot = base.ZooModel.Snapshot();
            var decay = new List<AnimalDecayDto>(snapshot.Animals.Count);

            foreach (var animal in snapshot.Animals)
            {
                decay.Add(new AnimalDecayDto(
                        animal.Id,
                        base.RandomProxy.NextPercentage(0, 20)));
            }

            base.ZooModel.AdvanceHour(new HourDecayDto(decay));

            return Complete();
        }
    }
}
