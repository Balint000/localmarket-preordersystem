namespace localmarket_preordersystem.Application.Common.Models
{
    /// <summary>Letölthető export (JSON/CSV) — az API ebből készít fájl-választ.</summary>
    public sealed record ExportFile(string FileName, string ContentType, byte[] Content);

    public enum ExportFormat { Json = 1, Csv = 2 }
}
