using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class RecordingMainView : IMainView
    {
        public event Action? ReadyEvt;
        public event Action? ResetRequestedEvt;

        public void RequestReset()
            => ResetRequestedEvt?.Invoke();

        public void RequestReady()
            => ReadyEvt?.Invoke();
    }
}
