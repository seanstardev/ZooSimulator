namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>An immutable authoritative time observation with bounded sub-hour presentation timing.</summary>
    public sealed class ZooTimeDto
    {
        /// <summary>Zoo state revision, allowing the view to reject stale asynchronous delivery.</summary>
        public long Revision { get; }

        /// <summary>Exact completed simulated hours since opening; initially zero.</summary>
        /// <remarks>Does not imply a clock reset when an unchanged hour is delivered again.</remarks>
        public long HoursElapsed { get; }

        /// <summary>Real milliseconds elapsed within the current twenty-second interval.</summary>
        public double MillisecondsIntoHour { get; }

        /// <summary>Real duration of one simulated hour, in milliseconds.</summary>
        public double HourDurationMilliseconds { get; }

        /// <summary>UTC observation instant for compensating local host-to-view delivery delay.</summary>
        public long ObservedAtUnixMilliseconds { get; }

        internal ZooTimeDto(
            long revision,
            long hoursElapsed,
            double millisecondsIntoHour,
            double hourDurationMilliseconds,
            long observedAtUnixMilliseconds)
        {
            Revision = revision;
            HoursElapsed = hoursElapsed;
            MillisecondsIntoHour = millisecondsIntoHour;
            HourDurationMilliseconds = hourDurationMilliseconds;
            ObservedAtUnixMilliseconds = observedAtUnixMilliseconds;
        }
    }
}
