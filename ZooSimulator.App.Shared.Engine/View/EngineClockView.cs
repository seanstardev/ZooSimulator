using System;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    internal sealed class EngineClockView : IClockView
    {
        private readonly IEngineTransport transport;

        internal EngineClockView(IEngineTransport transport)
        {
            this.transport = transport
                ?? throw new ArgumentNullException(nameof(transport));
        }

        public void ShowTime(ZooTimeDto time)
            => EngineProtocol.Send(transport, "clock", "show", time);
    }
}
