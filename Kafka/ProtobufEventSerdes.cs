using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Google.Protobuf;

namespace MaichessInsightsService.Kafka;

// Raw-Protobuf Kafka serde for the maichess.events.v1 generated messages. The maichess
// platform (Kafka task 09) dropped the Confluent Schema Registry: the wire format is the
// bare Protobuf bytes (msg.ToByteArray()), with schemas owned solely by
// maichess-api-contracts. Excluded from coverage (live serde glue).
[ExcludeFromCodeCoverage]
internal static class ProtobufEventSerdes
{
    public static ISerializer<T> Serializer<T>()
        where T : IMessage<T>
        => new RawSerializer<T>();

    private sealed class RawSerializer<T> : ISerializer<T>
        where T : IMessage<T>
    {
        public byte[] Serialize(T data, SerializationContext context) => data.ToByteArray();
    }
}
