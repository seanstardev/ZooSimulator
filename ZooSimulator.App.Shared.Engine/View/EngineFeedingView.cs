using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    internal sealed class EngineFeedingView : IFeedingView
    {
        public event Action? FeedRequestedEvt;
        public event Action<int>? AnimalFeedRequestedEvt;

        internal void FeedZoo()
            => FeedRequestedEvt?.Invoke();

        internal void FeedAnimal(int animalId)
            => AnimalFeedRequestedEvt?.Invoke(animalId);
    }
}
