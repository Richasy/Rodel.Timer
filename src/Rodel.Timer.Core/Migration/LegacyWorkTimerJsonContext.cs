using System.Text.Json.Serialization;

namespace Rodel.Timer.Core.Migration;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<LegacyFolderData>))]
[JsonSerializable(typeof(List<LegacyHistoryData>))]
internal sealed partial class LegacyWorkTimerJsonContext : JsonSerializerContext;
