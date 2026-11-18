using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class RecordingMainView : IMainView
    {
        public event Action? Ready;
        public event Action? ResetRequested;

        public void RequestReset()
            => ResetRequested?.Invoke();

        public void RequestReady()
            => Ready?.Invoke();
    }
}
