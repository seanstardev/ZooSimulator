using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    internal sealed class EngineFeedingView : IFeedingView
    {
        public event Action? FeedRequested;
        public event Action<int>? AnimalFeedRequested;

        internal void FeedZoo()
            => FeedRequested?.Invoke();

        internal void FeedAnimal(int animalId)
            => AnimalFeedRequested?.Invoke(animalId);
    }
}
