using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Stateless delivery of zoo snapshots to the enclosure view.</summary>
    internal sealed class ZooMediator : RMediator<IZooView>
    {
        public void ShowZoo(ZooSnapshotDto snapshot)
            => base.WithActor(
                view => view.ShowZoo(snapshot));

        protected override void ActorAttached(IZooView actor) { }

        protected override void ActorDetached(IZooView actor) { }
    }
}
