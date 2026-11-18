using System;

namespace ZooSimulator.App.Shared.Engine.Tests
{
    internal sealed class TestTransport : IEngineTransport
    {
        public event Action<string>? MessageReceivedEvt;

        public void PostMessage(string json) { }

        public void Receive(string json)
            => MessageReceivedEvt?.Invoke(json);
    }
}
