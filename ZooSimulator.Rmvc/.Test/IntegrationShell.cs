using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.Rmvc.Tests
{
    internal sealed class IntegrationShell : IRShell
    {
        public TaskCompletionSource<bool> Running { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<string> Errors { get; } = new();

        public void HandleStateChanged(RApplicationState state, string? message = null)
        {
            if (state == RApplicationState.Running)
                Running.TrySetResult(true);

            if (state == RApplicationState.Failed)
            {
                Errors.Enqueue(message ?? "Lifecycle failed.");
                Running.TrySetException(new InvalidOperationException(message));
            }
        }

        public bool ShowMessageBox(string title, string message, bool isYesNo = false)
        {
            Errors.Enqueue(title + ": " + message);

            return false;
        }
    }
}
