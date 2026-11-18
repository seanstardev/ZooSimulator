using System;
using ZooSimulator.App.Shared.Engine;

namespace ZooSimulator.App.Web
{
    /// <summary>Routes JSON on the single browser runtime's presentation context.</summary>
    internal sealed class BrowserTransport : IEngineTransport
    {
        public event Action<string>? MessageReceived;

        public void PostMessage(string json)
            => BrowserInterop.PostMessage(json);

        internal void Receive(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            MessageReceived?.Invoke(json);
        }
    }
}
