namespace localmarket_preordersystem.Application.Products
{
    /// <summary>A termékképek közös szabályai: méretkorlát, megengedett típusok, nyilvános URL.</summary>
    public static class ProductImages
    {
        public const int MaxBytes = 2 * 1024 * 1024;

        /// <summary>Az API ezen az útvonalon szolgálja ki a feltöltött képeket (statikus fájlként).</summary>
        public const string PublicPath = "/uploads/products";

        public static string? UrlFor(string? fileName)
            => string.IsNullOrWhiteSpace(fileName) ? null : $"{PublicPath}/{fileName}";

        /// <summary>
        /// A kiterjesztést a fájl TARTALMÁBÓL (magic bytes) állapítja meg, nem a kliens által megadott
        /// névből / content-type-ból. Null, ha nem JPEG, PNG vagy WebP.
        /// </summary>
        public static string? DetectExtension(ReadOnlySpan<byte> content)
        {
            if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
                return ".jpg";

            if (content.Length >= 8 &&
                content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
                content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
                return ".png";

            // WebP: "RIFF" <méret:4 bájt> "WEBP"
            if (content.Length >= 12 &&
                content[0] == 0x52 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x46 &&
                content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50)
                return ".webp";

            return null;
        }
    }
}
