using System;
using RMVC;

namespace ZooSimulator.Rmvc
{
    /// <summary>Owns a thread-safe pseudo-random source for inclusive integer percentages.</summary>
    internal sealed class RandomProxy : RModel, IRandomProxy
    {
        private readonly object gate = new object();
        private readonly System.Random random = new System.Random();

        public int NextPercentage(int minimumInclusive, int maximumInclusive)
        {
            if (minimumInclusive < 0 || maximumInclusive > 100 || minimumInclusive > maximumInclusive)
                throw new ArgumentOutOfRangeException(nameof(minimumInclusive));

            lock (gate)
                return random.Next(minimumInclusive, maximumInclusive + 1);
        }
    }
}
