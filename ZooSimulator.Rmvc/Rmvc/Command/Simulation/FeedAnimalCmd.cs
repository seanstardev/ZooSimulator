using System;
using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Feeds the drop target only; whole-zoo feeding remains a separate operation.</summary>
    internal sealed class FeedAnimalCmd : CommandBase<RVoid>
    {
        private readonly int animalId;

        public FeedAnimalCmd(int animalId)
        {
            if (animalId <= 0)
                throw new ArgumentOutOfRangeException(nameof(animalId));

            this.animalId = animalId;
        }

        protected override IRCommandLock[] GetLocks()
            => [ base.SimulationLock ];

        protected override Task<RVoid> Run()
        {
            var feeding = new AnimalFeedingDto(
                animalId,
                base.RandomProxy.NextPercentage(10, 25));

            base.ZooModel.FeedAnimal(feeding);

            return Complete();
        }
    }
}
