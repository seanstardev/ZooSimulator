using System;
using System.Threading.Tasks;
using RMVC;

namespace ZooSimulator.App.Web
{
    /// <summary>Browser lifecycle and dialogs; owns no zoo rules or actor lookup.</summary>
    internal sealed class BrowserShell : IRShell
    {
        private readonly TaskCompletionSource started = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Started
            => started.Task;

        public void HandleStateChanged(RApplicationState state, string? message = null)
        {
            if (state == RApplicationState.Running)
            {
                started.TrySetResult();
            }
            else if (state == RApplicationState.Failed)
            {
                string detail = message
                    ?? "The zoo application could not start.";

                started.TrySetException(new InvalidOperationException(detail));

                BrowserInterop.ReportDiagnostic(detail);
            }
        }

        public bool ShowMessageBox(string title, string message, bool isYesNo = false)
            => BrowserInterop.ShowMessageBox(title, message, isYesNo);
    }
}
