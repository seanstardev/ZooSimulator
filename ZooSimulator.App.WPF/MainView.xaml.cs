using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using RMVC;
using ZooSimulator.App.Shared.Engine;
using ZooSimulator.Rmvc.Shared;
using ZooFacade = ZooSimulator.Rmvc.Facade;

namespace ZooSimulator.App.WPF
{
    /// <summary>Owns WebView2 hosting and the four portable engine view adapters.</summary>
    public partial class MainView : UserControl, IDisposable
    {
        private GameEngine? engine;
        private WebViewTransport? transport;
        private bool started;
        private bool disposed;

        public MainView()
        {
            InitializeComponent();

            Browser.DefaultBackgroundColor = System.Drawing.Color.FromArgb(229, 234, 217);

            Loaded += OnLoaded;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Loaded -= OnLoaded;

            if (engine != null)
            {
                RFacade.UnregisterActor<ZooFacade, IMainView>(engine.MainView);
                RFacade.UnregisterActor<ZooFacade, IZooView>(engine.ZooView);
                RFacade.UnregisterActor<ZooFacade, IClockView>(engine.ClockView);
                RFacade.UnregisterActor<ZooFacade, IFeedingView>(engine.FeedingView);
                engine.DiagnosticEvt -= ShowStatus;
                engine.Dispose();
            }

            transport?.Dispose();
            Browser.Dispose();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (started || disposed)
                return;

            started = true;

            SceneVerification.Trace("MainView loaded");

            try
            {
                SceneVerification.Trace("Exporting assets");

                string folder = await Task.Run(()
                    => EngineAssetExporter.Export(AppContext.BaseDirectory));

                if (disposed)
                    return;

                SceneVerification.Trace("Creating WebView environment");

                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: Path.Combine(AppContext.BaseDirectory, ".WebView2"));

                if (disposed)
                    return;

                SceneVerification.Trace("Creating WebView control");

                await Browser.EnsureCoreWebView2Async(environment);

                SceneVerification.Trace("WebView created");

                if (disposed)
                    return;

                Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "zoo.local",
                    folder,
                    CoreWebView2HostResourceAccessKind.DenyCors);

                Browser.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                Browser.CoreWebView2.NavigationStarting += OnNavigationStarting;
                Browser.CoreWebView2.NewWindowRequested += OnNewWindow;

                transport = new WebViewTransport(Browser.CoreWebView2, Dispatcher);
                engine = new GameEngine(transport);
                engine.DiagnosticEvt += ShowStatus;

                RFacade.RegisterActor<ZooFacade, IMainView>(engine.MainView);
                RFacade.RegisterActor<ZooFacade, IZooView>(engine.ZooView);
                RFacade.RegisterActor<ZooFacade, IClockView>(engine.ClockView);
                RFacade.RegisterActor<ZooFacade, IFeedingView>(engine.FeedingView);

                SceneVerification.Trace("Navigating to scene");

                Browser.Source = new Uri("https://zoo.local/index.html");
                ShowStatus("Loading the animal models…");

                string[] arguments = Environment.GetCommandLineArgs();

                int verification = Array.IndexOf(arguments, "--verify-scene");

                if (verification >= 0 && verification + 1 < arguments.Length)
                {
                    await SceneVerification.Run(Browser.CoreWebView2, arguments[verification + 1]);

                    ShowStatus("Scene verification captured to " + arguments[verification + 1]);
                }
            }
            catch (Exception error)
            {
                SceneVerification.Trace(error.ToString());

                ShowStatus("Scene could not start: " + error.Message);

                MessageBox.Show(
                    "The scene could not start. Check the WebView2 Runtime and writable app directory.\n\n" + error.Message,
                    "Zoo Simulator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var uri = new Uri(e.Uri);

            e.Cancel = uri.Scheme != "https" || uri.Host != "zoo.local" || uri.IsDefaultPort == false;
        }

        private void OnNewWindow(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
            => e.Handled = true;

        private void ShowStatus(string text)
            => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (disposed == false && DataContext is MainViewModel viewModel)
                    viewModel.Status = text;
            }));
    }
}
