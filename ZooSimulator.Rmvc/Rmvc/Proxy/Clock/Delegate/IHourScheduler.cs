using System;

namespace ZooSimulator.Rmvc.Proxy.Clock
{
    /// <summary>Clock-owned scheduler contract; implementations release their own timer resources.</summary>
    internal interface IHourScheduler : IDisposable
    {
        event Action<long>? Tick;
        void Start(TimeSpan interval, long scheduleVersion);
    }
}
