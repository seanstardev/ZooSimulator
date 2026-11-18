using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RMVC;
using ZooSimulator.Rmvc.Proxy.Clock;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Owns the twenty-second scheduler and submits one operation per delivered interval.</summary>
    internal sealed class ClockProxy : RModel, IClockProxy
    {
        internal static readonly TimeSpan HourInterval = TimeSpan.FromSeconds(20);

        private readonly object gate = new object();
        private readonly IHourScheduler scheduler;
        private readonly Stopwatch elapsed = new Stopwatch();
        private bool started;
        private long scheduleVersion;
        private bool stopped;

        public ClockProxy() : this(new TimerScheduler()) { }

        internal ClockProxy(IHourScheduler scheduler)
        {
            this.scheduler = scheduler
                ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public void Start()
            => Configure(false);

        public void Restart()
            => Configure(true);

        public bool IsCurrentSchedule(long version)
        {
            lock (gate)
                return stopped == false && version == scheduleVersion;
        }

        /// <summary>Pairs committed whole hours with monotonic, bounded interval progress.</summary>
        public ZooTimeDto ObserveTime(long revision, long hoursElapsed)
        {
            lock (gate)
            {
                double progress = Math.Max(0, Math.Min(
                    HourInterval.TotalMilliseconds,
                    elapsed.Elapsed.TotalMilliseconds - hoursElapsed * HourInterval.TotalMilliseconds));

                return new ZooTimeDto(
                    revision,
                    hoursElapsed,
                    progress,
                    HourInterval.TotalMilliseconds,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }

        /// <summary>Stops future submissions and releases the owned scheduler, including partial startup.</summary>
        public override Task Cleanup()
        {
            lock (gate)
            {
                if (stopped)
                    return Task.CompletedTask;

                stopped = true;
                scheduler.TickEvt -= OnTick;
                scheduler.Dispose();
            }

            return Task.CompletedTask;
        }

        private void Configure(bool restart)
        {
            lock (gate)
            {
                if (stopped)
                {
                    throw new InvalidOperationException(
                        "The clock has been cleaned up.");
                }

                if (started && restart == false)
                    return;

                if (started == false)
                {
                    started = true;
                    scheduler.TickEvt += OnTick;
                }

                if (restart)
                    scheduleVersion++;

                elapsed.Restart();
                scheduler.Start(HourInterval, scheduleVersion);
            }
        }

        private void OnTick(long version)
        {
            lock (gate)
            {
                if (stopped == false && version == scheduleVersion)
                    base.Execute(new AdvanceHourCmd(version));
            }
        }
    }
}
