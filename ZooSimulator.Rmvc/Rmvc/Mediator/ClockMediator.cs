using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Stateless delivery of authoritative time observations to the clock view.</summary>
    internal sealed class ClockMediator : RMediator<IClockView>
    {
        public void ShowTime(ZooTimeDto time)
            => base.WithActor(
                view => view.ShowTime(time));

        protected override void ActorAttached(IClockView actor) { }

        protected override void ActorDetached(IClockView actor) { }
    }
}
