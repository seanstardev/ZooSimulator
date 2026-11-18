using System;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using ZooSimulator.App.Shared.Engine;

namespace ZooSimulator.App.WPF
{
    /// <summary>WPF-specific transport; portable engine code never references WebView2.</summary>
    internal sealed class WebViewTransport : IEngineTransport, IDisposable
    {
        private readonly CoreWebView2 browser;
        private readonly Dispatcher dispatcher;
        private bool disposed;

        public event Action<string>? MessageReceived;

        internal WebViewTransport(CoreWebView2 browser, Dispatcher dispatcher)
        {
            this.browser = browser
                ?? throw new ArgumentNullException(nameof(browser));
            this.dispatcher = dispatcher
                ?? throw new ArgumentNullException(nameof(dispatcher));

            browser.WebMessageReceived += OnMessage;
        }

        public void PostMessage(string json)
            => dispatcher.BeginInvoke(new Action(() =>
            {
                if (disposed == false)
                    browser.PostWebMessageAsJson(json);
            }));

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            browser.WebMessageReceived -= OnMessage;
        }

        private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (disposed || Uri.TryCreate(e.Source, UriKind.Absolute, out var source) == false)
                return;

            if (source.Scheme == "https" && source.Host == "zoo.local" && source.IsDefaultPort)
                MessageReceived?.Invoke(e.WebMessageAsJson);
        }
    }
}
