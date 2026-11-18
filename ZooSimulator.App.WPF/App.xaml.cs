using System.Windows;
using RMVC;

namespace ZooSimulator.App.WPF
{
    /// <summary>Composes the native shell and portable RMVC application.</summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SceneVerification.Trace("Application starting");

            base.OnStartup(e);

            var window = new MainWindow();

            MainWindow = window;

            RFacade.Create<ZooSimulator.Rmvc.Facade>(window);

            window.Show();

            SceneVerification.Trace("Window shown");
        }
    }
}
