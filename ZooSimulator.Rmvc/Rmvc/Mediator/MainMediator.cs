using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Translates composition readiness and fresh-game intent into application operations.</summary>
    internal sealed class MainMediator : RMediator<IMainView>
    {
        protected override void ActorAttached(IMainView actor)
        {
            actor.ReadyEvt += OnReady;
            actor.ResetRequestedEvt += OnResetRequested;
        }

        protected override void ActorDetached(IMainView actor)
        {
            actor.ReadyEvt -= OnReady;
            actor.ResetRequestedEvt -= OnResetRequested;
        }

        private void OnResetRequested()
            => base.Execute(new ResetZooCmd());

        private void OnReady()
            => base.Execute(new StartupCmd());
    }
}
