using System;

namespace ZooSimulator.App.Shared.Engine.Tests
{
    internal sealed class TestTransport : IEngineTransport
    {
        public event Action<string>? MessageReceived;

        public void PostMessage(string json) { }

        public void Receive(string json)
            => MessageReceived?.Invoke(json);
    }
}
