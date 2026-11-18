using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Translates whole-zoo and targeted feeding intents into their distinct operations.</summary>
    internal sealed class FeedingMediator : RMediator<IFeedingView>
    {
        protected override void ActorAttached(IFeedingView actor)
        {
            actor.FeedRequested += OnFeedRequested;
            actor.AnimalFeedRequested += OnAnimalFeedRequested;
        }

        protected override void ActorDetached(IFeedingView actor)
        {
            actor.FeedRequested -= OnFeedRequested;
            actor.AnimalFeedRequested -= OnAnimalFeedRequested;
        }

        private void OnAnimalFeedRequested(int animalId)
            => base.Execute(new FeedAnimalCmd(animalId));

        private void OnFeedRequested()
            => base.Execute(new FeedZooCmd());
    }
}
