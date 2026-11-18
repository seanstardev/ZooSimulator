using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.App.Shared.Engine
{
    /// <summary>Owns four thin RMVC view adapters and routes trusted browser intents.</summary>
    /// <remarks>The transport is borrowed; disposing the engine never disposes its host.</remarks>
    public sealed class GameEngine : IDisposable
    {
        private readonly IEngineTransport transport;
        private readonly EngineMainView main = new EngineMainView();
        private readonly EngineFeedingView feeding = new EngineFeedingView();
        private bool disposed;

        /// <summary>Composition readiness adapter, separate from the other actor contracts.</summary>
        public IMainView MainView { get; }

        /// <summary>Enclosure presentation adapter.</summary>
        public IZooView ZooView { get; }

        /// <summary>Clock presentation adapter.</summary>
        public IClockView ClockView { get; }

        /// <summary>Whole-zoo and targeted feeding intent adapter.</summary>
        public IFeedingView FeedingView { get; }

        /// <summary>Reports renderer/protocol diagnostics for host presentation.</summary>
        public event Action<string>? DiagnosticEvt;

        /// <summary>Creates adapters before browser navigation so readiness cannot be missed.</summary>
        public GameEngine(IEngineTransport transport)
        {
            this.transport = transport
                ?? throw new ArgumentNullException(nameof(transport));

            MainView = main;
            ZooView = new EngineZooView(transport);
            ClockView = new EngineClockView(transport);
            FeedingView = feeding;

            transport.MessageReceivedEvt += OnMessage;
        }

        /// <summary>Disconnects incoming browser intents; the host separately disposes WebView2.</summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            transport.MessageReceivedEvt -= OnMessage;
        }

        private void OnMessage(string json)
        {
            if (disposed)
                return;

            try
            {
                var message = JObject.Parse(json);

                if (message["version"]?.Type != JTokenType.Integer || message.Value<long>("version") != 1)
                {
                    throw new JsonException(
                        "Unsupported engine protocol version.");
                }

                if (message["actor"]?.Type != JTokenType.String || message["event"]?.Type != JTokenType.String)
                {
                    throw new JsonException(
                        "An engine message requires string actor and event names.");
                }

                string? actor = message.Value<string>("actor");
                string? intent = message.Value<string>("event");

                if (actor == "main" && (intent == "error" || intent == "status") &&
                    message["message"] is JToken detail &&
                    detail.Type != JTokenType.String && detail.Type != JTokenType.Null)
                {
                    throw new JsonException(
                        "A scene diagnostic must contain a string message.");
                }

                if (actor == "main" && intent == "ready")
                {
                    main.NotifyReady();
                }
                else if (actor == "main" && intent == "reset")
                {
                    main.NotifyReset();
                }
                else if (actor == "feeding" && intent == "feedZoo")
                {
                    feeding.FeedZoo();
                }
                else if (actor == "feeding" && intent == "feedAnimal")
                {
                    if (message["animalId"]?.Type != JTokenType.Integer)
                    {
                        throw new JsonException(
                            "A targeted feeding requires an integer animal identifier.");
                    }

                    long suppliedId = message.Value<long>("animalId");

                    if (suppliedId < 1 || suppliedId > 15)
                    {
                        throw new JsonException(
                            "The animal identifier is outside this zoo.");
                    }

                    int id = (int)suppliedId;

                    feeding.FeedAnimal(id);
                }
                else if (actor == "main" && intent == "error")
                {
                    DiagnosticEvt?.Invoke(message.Value<string>("message")
                        ?? "The scene reported an unspecified error.");
                }
                else if (actor == "main" && intent == "status")
                {
                    DiagnosticEvt?.Invoke(message.Value<string>("message")
                        ?? "Scene ready.");
                }
                else
                {
                    throw new JsonException(
                        "Unknown engine actor or event.");
                }
            }
            catch (Exception error) when (
                error is JsonException
                || error is FormatException
                || error is OverflowException)
            {
                DiagnosticEvt?.Invoke("Engine message rejected: " + error.Message);
            }
        }
    }
}
