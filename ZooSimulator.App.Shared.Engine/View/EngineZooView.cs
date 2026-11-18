using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    internal sealed class EngineZooView : IZooView
    {
        private readonly IEngineTransport transport;

        internal EngineZooView(IEngineTransport transport)
        {
            this.transport = transport
                ?? throw new ArgumentNullException(nameof(transport));
        }

        public void ShowZoo(ZooSnapshotDto snapshot)
            => EngineProtocol.Send(transport, "zoo", "show", snapshot);
    }
}
