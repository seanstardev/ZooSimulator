using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class RecordingFeedingView : IFeedingView
    {
        public event Action? FeedRequested;
        public event Action<int>? AnimalFeedRequested;

        public int SubscriberCount
            => FeedRequested?.GetInvocationList().Length
                ?? 0;

        public void RequestAnimalFeed(int animalId)
            => AnimalFeedRequested?.Invoke(animalId);

        public void RequestFeed()
            => FeedRequested?.Invoke();
    }
}
