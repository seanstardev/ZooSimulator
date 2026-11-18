using System;
using System.ComponentModel;
using System.Windows;
using RMVC;

namespace ZooSimulator.App.WPF
{
    /// <summary>Thin native shell: message boxes, lifecycle status and orderly shutdown.</summary>
    public partial class MainWindow : Window, IRShell
    {
        private readonly MainViewModel viewModel = new MainViewModel();
        private bool closing;
        private bool closeAllowed;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = viewModel;
            Closing += OnClosing;
        }

        public void HandleStateChanged(RApplicationState state, string? message = null)
        {
            string detail = message == null
                ? string.Empty
                : " — " + message;

            Dispatcher.BeginInvoke(new Action(
                () => viewModel.Status = "Application: " + state + detail));
        }

        public bool ShowMessageBox(string title, string message, bool isYesNo = false)
        {
            var buttons = isYesNo
                ? MessageBoxButton.YesNo
                : MessageBoxButton.OK;

            return Dispatcher.Invoke(() => MessageBox.Show(
                this,
                message,
                title,
                buttons,
                MessageBoxImage.Information) != MessageBoxResult.No);
        }

        private async void OnClosing(object? sender, CancelEventArgs e)
        {
            if (closeAllowed)
                return;

            e.Cancel = true;

            if (closing)
                return;

            closing = true;
            SceneView.IsEnabled = false;

            try
            {
                await RFacade.Shutdown(10000);

                closeAllowed = true;
            }
            catch (Exception error)
            {
                closeAllowed = MessageBox.Show(
                    this,
                    "Shutdown failed: " + error.Message + "\nClose the host anyway?",
                    "Zoo shutdown",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error) == MessageBoxResult.Yes;
            }

            if (closeAllowed)
            {
                SceneView.Dispose();
                Close();
            }
            else
                closing = false;
        }
    }
}
