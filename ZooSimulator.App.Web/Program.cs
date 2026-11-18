using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using RMVC;
using ZooSimulator.App.Shared.Engine;
using ZooSimulator.Rmvc.Shared;
using ZooFacade = ZooSimulator.Rmvc.Facade;

[assembly: SupportedOSPlatform("browser")]

namespace ZooSimulator.App.Web
{
    /// <summary>Composes the existing application and Engine inside the browser.</summary>
    /// <remarks>Partial is required by the JavaScript interop source generator.</remarks>
    internal static partial class Program
    {
        private static readonly BrowserTransport transport = new BrowserTransport();
        private static GameEngine? engine;
        private static bool stopping;

        public static async Task Main()
        {
            var shell = new BrowserShell();

            engine = new GameEngine(transport);
            engine.Diagnostic += BrowserInterop.ReportDiagnostic;
            RFacade.Create<ZooFacade>(shell);

            await shell.Started;

            RFacade.RegisterActor<ZooFacade, IMainView>(engine.MainView);
            RFacade.RegisterActor<ZooFacade, IZooView>(engine.ZooView);
            RFacade.RegisterActor<ZooFacade, IClockView>(engine.ClockView);
            RFacade.RegisterActor<ZooFacade, IFeedingView>(engine.FeedingView);
        }

        /// <summary>Accepts an intent from this document's local renderer bridge.</summary>
        [JSExport]
        internal static void Receive(string json)
        {
            if (stopping == false)
                transport.Receive(json);
        }

        /// <summary>Best-effort cleanup; browsers do not guarantee async unload completion.</summary>
        [JSExport]
        internal static async Task Shutdown()
        {
            if (stopping)
                return;

            stopping = true;

            await RFacade.Shutdown(2000);

            engine?.Dispose();
        }
    }
}
