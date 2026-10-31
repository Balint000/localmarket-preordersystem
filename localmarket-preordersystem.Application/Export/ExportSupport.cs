using System.Text.Json;
using System.Text.Json.Serialization;

namespace localmarket_preordersystem.Application.Export
{
    internal static class ExportSupport
    {
        public static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
