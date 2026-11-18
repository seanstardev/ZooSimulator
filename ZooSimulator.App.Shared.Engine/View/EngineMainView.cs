using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    internal sealed class EngineMainView : IMainView
    {
        public event Action? ReadyEvt;
        public event Action? ResetRequestedEvt;

        internal void NotifyReset()
            => ResetRequestedEvt?.Invoke();

        internal void NotifyReady()
            => ReadyEvt?.Invoke();
    }
}
