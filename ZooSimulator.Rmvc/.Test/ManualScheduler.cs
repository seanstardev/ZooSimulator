using System;
using System.Threading;
using ZooSimulator.Rmvc.Proxy.Clock;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class ManualScheduler : IHourScheduler
    {
        private int startCount;

        public event Action<long>? Tick;
        public long ScheduleVersion { get; private set; }
        public TimeSpan Interval { get; private set; }
        public int StartCount => Volatile.Read(ref startCount);
        public int SubscriberCount => Tick?.GetInvocationList().Length ?? 0;
        public bool Disposed { get; private set; }
        public int DisposeCount { get; private set; }

        public void Start(TimeSpan interval, long scheduleVersion)
        {
            Interval = interval;
            ScheduleVersion = scheduleVersion;
            Interlocked.Increment(ref startCount);
        }

        public void Fire(long? version = null)
            => Tick?.Invoke(version ?? ScheduleVersion);

        public void Dispose()
        {
            Disposed = true;
            DisposeCount++;
        }
    }
}
