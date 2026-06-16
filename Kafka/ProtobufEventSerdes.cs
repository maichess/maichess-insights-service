using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;

namespace MaichessInsightsService.Kafka;

// Confluent Protobuf serde factory for the maichess.events.v1 generated messages — the
// same pattern as anticheat / match-manager. Excluded from coverage (live serde glue).
[ExcludeFromCodeCoverage]
internal static class ProtobufEventSerdes
{
    public static IAsyncSerializer<T> Serializer<T>(ISchemaRegistryClient registry)
        where T : class, IMessage<T>, new()
        => new ProtobufSerializer<T>(registry);
}
