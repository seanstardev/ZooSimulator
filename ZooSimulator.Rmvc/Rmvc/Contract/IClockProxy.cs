using RMVC;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc
{
    /// <summary>Owns scheduling of simulated hours, not the zoo's elapsed-hour state.</summary>
    internal interface IClockProxy : IRModel
    {
        /// <summary>Starts twenty-second intervals once; repeated calls do not reset the schedule.</summary>
        void Start();

        /// <summary>Begins a fresh full interval and invalidates previous scheduled callbacks.</summary>
        void Restart();

        /// <summary>Observes monotonic interval progress alongside a committed zoo hour/revision.</summary>
        ZooTimeDto ObserveTime(long revision, long hoursElapsed);

        /// <summary>Rejects queued hourly commands from a superseded schedule.</summary>
        bool IsCurrentSchedule(long version);
    }
}
