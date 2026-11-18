using System.Collections.Concurrent;
using System.Threading;
using RMVC;
using Xunit;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class ScriptedRandomProxy : RModel, IRandomProxy
    {
        private readonly ConcurrentQueue<int> values = new();
        private int calls;

        public int Calls
            => Volatile.Read(ref calls);

        public int Remaining
            => values.Count;
        public ConcurrentQueue<int> Minimums { get; } = new();
        public ConcurrentQueue<int> Maximums { get; } = new();

        public void Enqueue(params int[] percentages)
        {
            foreach (int percentage in percentages)
                values.Enqueue(percentage);
        }

        public int NextPercentage(int minimumInclusive, int maximumInclusive)
        {
            Assert.True(
                values.TryDequeue(out int result),
                "Unexpected random draw.");
            Assert.InRange(result, minimumInclusive, maximumInclusive);
            Minimums.Enqueue(minimumInclusive);
            Maximums.Enqueue(maximumInclusive);
            Interlocked.Increment(ref calls);

            return result;
        }
    }
}
