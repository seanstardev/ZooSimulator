using System.Runtime.InteropServices.JavaScript;

namespace ZooSimulator.App.Web
{
    /// <summary>Document-local interop; no rendering or simulation policy lives here.</summary>
    /// <remarks>Partial is required by the JavaScript interop source generator.</remarks>
    internal static partial class BrowserInterop
    {
        [JSImport("globalThis.zooBrowserPostMessage")]
        internal static partial void PostMessage(string json);

        [JSImport("globalThis.zooBrowserDiagnostic")]
        internal static partial void ReportDiagnostic(string message);

        [JSImport("globalThis.zooBrowserMessageBox")]
        internal static partial bool ShowMessageBox(string title, string message, bool isYesNo);
    }
}
