using System;
using System.Threading;

namespace ZooSimulator.Rmvc.Proxy.Clock
{
    /// <summary>Schedules periodic callbacks independently of rendering and UI dispatch.</summary>
    internal sealed class TimerScheduler : IHourScheduler
    {
        private Timer? timer;

        public event Action<long>? TickEvt;

        public void Start(TimeSpan interval, long scheduleVersion)
        {
            timer?.Dispose();
            timer = new Timer(OnTick, scheduleVersion, interval, interval);
        }

        public void Dispose()
        {
            timer?.Dispose();
            timer = null;
        }

        private void OnTick(object? state)
            => TickEvt?.Invoke((long)state!);
    }
}
