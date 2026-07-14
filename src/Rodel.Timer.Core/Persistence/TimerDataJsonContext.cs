using System.Text.Json.Serialization;

namespace Rodel.Timer.Core.Persistence;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true,
    WriteIndented = true)]
[JsonSerializable(typeof(TimerDataDocument))]
internal sealed partial class TimerDataJsonContext : JsonSerializerContext;
