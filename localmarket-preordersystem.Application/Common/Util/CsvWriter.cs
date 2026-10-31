using System.Globalization;
using System.Text;

namespace localmarket_preordersystem.Application.Common.Util
{
    /// <summary>
    /// Minimális CSV-író magyar Excelhez: pontosvessző elválasztó, tizedesvessző, UTF-8 BOM.
    /// A számformátum saját NumberFormatInfo, így nem függ a szerver ICU/kultúra-beállításától.
    /// </summary>
    public sealed class CsvWriter
    {
        private static readonly NumberFormatInfo Numbers = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = "" };
        private readonly StringBuilder _sb = new();

        public CsvWriter Row(params object?[] cells)
        {
            _sb.Append(string.Join(';', cells.Select(Format))).Append("\r\n");
            return this;
        }

        public CsvWriter BlankLine() { _sb.Append("\r\n"); return this; }

        public byte[] ToBytes()
        {
            var enc = new UTF8Encoding(true);
            return enc.GetPreamble().Concat(enc.GetBytes(_sb.ToString())).ToArray();
        }

        private static string Format(object? value)
        {
            var text = value switch
            {
                null => "",
                decimal d => d.ToString("0.##", Numbers),
                double d => d.ToString("0.##", Numbers),
                DateTime dt => dt.ToString("yyyy-MM-dd"),
                TimeOnly t => t.ToString("HH:mm"),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };

            // CSV-injekció ellen: képletnek látszó cella elé aposztróf (a szám nem érintett)
            if (value is string && text.Length > 0 && "=+-@".Contains(text[0]))
                text = "'" + text;

            return text.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
        }
    }
}
