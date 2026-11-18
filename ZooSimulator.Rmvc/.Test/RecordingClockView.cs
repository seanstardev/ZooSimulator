using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class RecordingClockView : IClockView
    {
        private readonly object gate = new();

        private readonly ConcurrentDictionary<long, TaskCompletionSource<ZooTimeDto>> revisions = new();

        private long displayedRevision = -1;

        public void ShowTime(ZooTimeDto time)
        {
            lock (gate)
            {
                if (time.Revision < displayedRevision)
                    return;

                displayedRevision = time.Revision;
                Completion(time.Revision).TrySetResult(time);
            }
        }

        public Task<ZooTimeDto> WaitForRevision(long revision)
            => Completion(revision).Task.WaitAsync(TimeSpan.FromSeconds(10));

        private TaskCompletionSource<ZooTimeDto> Completion(long revision)
            => revisions.GetOrAdd(
                revision,
                _ => new TaskCompletionSource<ZooTimeDto>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
    }
}
