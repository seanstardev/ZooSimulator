using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ZooSimulator.App.Shared.Engine.Tests
{
    public sealed class EngineTests
    {
        [Fact]
        public void ExportContainsOfflineSceneModelsAndExactVerifiedVendorBytes()
        {
            string sandbox = Path.Combine(Path.GetTempPath(), "ZooSimulator.Tests", Guid.NewGuid().ToString("N"));

            string folder = EngineAssetExporter.Export(sandbox);

            Assert.Equal(Path.Combine(sandbox, "Engine"), folder);
            Assert.True(File.Exists(Path.Combine(folder, "index.html")));
            Assert.True(File.Exists(Path.Combine(folder, "scene.js")));
            Assert.True(File.Exists(Path.Combine(folder, "licenses", "Kenney-Cube-Pets.txt")));

            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "asset-manifest.json")));

            foreach (var file in manifest["files"]!)
            {
                string path = Path.Combine(folder, file.Value<string>("path")!);
                string expected = file.Value<string>("sha256")!;

                string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void BridgeRoutesKnownIntentsRejectsMalformedInputAndDisconnectsOnDispose()
        {
            var transport = new TestTransport();

            using var engine = new GameEngine(transport);

            int ready = 0;
            int resets = 0;
            int feeds = 0;
            int target = 0;
            var diagnostics = new List<string>();

            engine.MainView.ReadyEvt += () => ready++;
            engine.MainView.ResetRequestedEvt += () => resets++;
            engine.FeedingView.FeedRequestedEvt += () => feeds++;
            engine.FeedingView.AnimalFeedRequestedEvt += id => target = id;
            engine.DiagnosticEvt += diagnostics.Add;

            transport.Receive("{\"version\":1,\"actor\":\"main\",\"event\":\"ready\"}");
            transport.Receive("{\"version\":1,\"actor\":\"feeding\",\"event\":\"feedZoo\"}");
            transport.Receive("{\"version\":1,\"actor\":\"feeding\",\"event\":\"feedAnimal\",\"animalId\":7}");

            Assert.Equal(1, ready);

            transport.Receive("{\"version\":1,\"actor\":\"main\",\"event\":\"reset\"}");

            Assert.Equal(1, resets);
            Assert.Equal(1, feeds);
            Assert.Equal(7, target);
            Assert.NotSame(engine.MainView, engine.ZooView);
            Assert.NotSame(engine.MainView, engine.FeedingView);

            transport.Receive("not json");
            transport.Receive("{\"version\":\"bad\"}");
            transport.Receive("{\"version\":1,\"actor\":\"feeding\",\"event\":\"feedAnimal\",\"animalId\":99}");
            transport.Receive("{\"version\":1,\"actor\":\"feeding\",\"event\":\"feedAnimal\",\"animalId\":\"oops\"}");

            Assert.Equal(4, diagnostics.Count);
            Assert.Equal(7, target);

            engine.Dispose();
            transport.Receive("{\"version\":1,\"actor\":\"feeding\",\"event\":\"feedZoo\"}");

            Assert.Equal(1, feeds);
        }

        [Fact]
        public void RequiredHostDependenciesAreRejectedAtConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new GameEngine(null!));
            Assert.Throws<ArgumentNullException>(() => EngineAssetExporter.Export(null!));
            Assert.Throws<ArgumentException>(() => EngineAssetExporter.Export("relative-folder"));
        }

        [Theory]
        [InlineData("{\"version\":1,\"actor\":{},\"event\":\"ready\"}")]
        [InlineData("{\"version\":1,\"actor\":\"main\",\"event\":[]}")]
        [InlineData("{\"version\":1,\"actor\":\"main\",\"event\":\"status\",\"message\":{}}")]
        [InlineData("{\"version\":1,\"actor\":\"main\",\"event\":\"error\",\"message\":[]}")]
        public void MalformedMessageShapesDoNotBreakSubsequentReadiness(string json)
        {
            var transport = new TestTransport();

            using var engine = new GameEngine(transport);

            var diagnostics = new List<string>();
            int ready = 0;

            engine.DiagnosticEvt += diagnostics.Add;
            engine.MainView.ReadyEvt += () => ready++;

            transport.Receive(json);
            transport.Receive("{\"version\":1,\"actor\":\"main\",\"event\":\"ready\"}");

            Assert.Single(diagnostics);
            Assert.Equal(1, ready);
        }
    }
}
