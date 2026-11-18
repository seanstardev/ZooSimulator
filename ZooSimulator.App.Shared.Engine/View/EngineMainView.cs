using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    internal sealed class EngineMainView : IMainView
    {
        public event Action? Ready;
        public event Action? ResetRequested;

        internal void NotifyReset()
            => ResetRequested?.Invoke();

        internal void NotifyReady()
            => Ready?.Invoke();
    }
}
