using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Generates exactly one increase per species and feeds the zoo atomically.</summary>
    internal sealed class FeedZooCmd : CommandBase<RVoid>
    {
        protected override IRCommandLock[] GetLocks()
            => [ base.SimulationLock ];

        protected override Task<RVoid> Run()
        {
            var feeding = new FeedingDto(
                base.RandomProxy.NextPercentage(10, 25),
                base.RandomProxy.NextPercentage(10, 25),
                base.RandomProxy.NextPercentage(10, 25));

            base.ZooModel.Feed(feeding);

            return Complete();
        }
    }
}
