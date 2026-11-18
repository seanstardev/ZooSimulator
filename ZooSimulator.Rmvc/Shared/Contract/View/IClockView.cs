using RMVC;

namespace ZooSimulator.Rmvc.Shared
{
    /// <summary>Independent visual presentation of authoritative zoo time.</summary>
    public interface IClockView : IRContract
    {
        /// <summary>Displays completed simulated hours and measured interval timing.</summary>
        /// <remarks>
        /// The view chooses its visual representation and owns dispatch. Compare
        /// revisions on its presentation context and ignore older deliveries. This
        /// observation includes interval progress, duration and a reference timestamp
        /// for local minute/second interpolation. Do not display a new hour until
        /// the application confirms its completion.
        /// Visual animation must not schedule health changes or assume a new hour
        /// begins whenever this method is called; feeding can deliver the same hour.
        /// </remarks>
        void ShowTime(ZooTimeDto time);
    }
}
