using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class RecordingFeedingView : IFeedingView
    {
        public event Action? FeedRequestedEvt;
        public event Action<int>? AnimalFeedRequestedEvt;

        public int SubscriberCount
            => FeedRequestedEvt?.GetInvocationList().Length
                ?? 0;

        public void RequestAnimalFeed(int animalId)
            => AnimalFeedRequestedEvt?.Invoke(animalId);

        public void RequestFeed()
            => FeedRequestedEvt?.Invoke();
    }
}
