using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ZooSimulator.App.Shared.Engine
{
    /// <summary>Versioned messages addressed to named JavaScript presentation actors.</summary>
    internal static class EngineProtocol
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        internal static void Send(
            IEngineTransport transport,
            string actor,
            string operation,
            object data)
            => transport.PostMessage(JsonConvert.SerializeObject(
                new
                {
                    version = 1, actor, operation, data
                },
                settings));
    }
}
