using System;

namespace ZooSimulator.App.Shared.Engine
{
    /// <summary>Host-owned JSON transport; the host owns UI dispatch and origin validation.</summary>
    public interface IEngineTransport
    {
        /// <summary>Delivers one message from the trusted engine document.</summary>
        event Action<string>? MessageReceived;

        /// <summary>Posts JSON to the engine document on its presentation context.</summary>
        void PostMessage(string json);
    }
}
