using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class RecordingView : IZooView
    {
        private readonly object gate = new();

        private readonly ConcurrentDictionary<long, TaskCompletionSource<ZooSnapshotDto>> revisions = new();

        private long displayedRevision = -1;

        public ConcurrentQueue<ZooSnapshotDto> Deliveries { get; } = new();

        public void ShowZoo(ZooSnapshotDto snapshot)
        {
            Deliveries.Enqueue(snapshot);

            lock (gate)
            {
                if (snapshot.Revision < displayedRevision)
                    return;

                displayedRevision = snapshot.Revision;
                Completion(snapshot.Revision).TrySetResult(snapshot);
            }
        }

        public Task<ZooSnapshotDto> WaitForRevision(long revision)
            => Completion(revision).Task.WaitAsync(TimeSpan.FromSeconds(10));

        private TaskCompletionSource<ZooSnapshotDto> Completion(long revision)
            => revisions.GetOrAdd(
                revision,
                _ => new TaskCompletionSource<ZooSnapshotDto>(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}
