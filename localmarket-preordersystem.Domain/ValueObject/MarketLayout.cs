using localmarket_preordersystem.Domain.Exceptions;

namespace localmarket_preordersystem.Domain.ValueObject
{
    /// <summary>
    /// Egy stand a piac felülnézeti rácsán. A <see cref="Number"/> a stand azonosítója a piacon belül;
    /// ezt kapja meg az árus (Producer.StallNumber). Az (X, Y) a bal felső cella, a méret cellákban értendő.
    /// </summary>
    public sealed record MarketStall(int Number, int X, int Y, int Width = 1, int Height = 1, string? Label = null);

    /// <summary>
    /// A piac elrendezése felülnézetből: egy Columns × Rows méretű rács, rajta egymást nem átfedő standok.
    /// Értékobjektum: módosítani csak újat létrehozva lehet (<see cref="Create"/>), ami minden szabályt ellenőriz.
    /// </summary>
    public sealed class MarketLayout
    {
        public const int MinSize = 4;
        public const int MaxColumns = 40;
        public const int MaxRows = 30;
        public const int MaxStallSize = 6;
        public const int MaxStalls = 200;
        public const int MaxStallNumber = 999;
        public const int MaxLabelLength = 40;

        public int Columns { get; }
        public int Rows { get; }
        public IReadOnlyList<MarketStall> Stalls { get; }

        private MarketLayout(int columns, int rows, IReadOnlyList<MarketStall> stalls)
        {
            Columns = columns;
            Rows = rows;
            Stalls = stalls;
        }

        /// <summary>Üres, 12 × 8-as rács (új piacnál; a piacvezető ebből indul).</summary>
        public static MarketLayout Empty { get; } = new(12, 8, Array.Empty<MarketStall>());

        public static MarketLayout Create(int columns, int rows, IEnumerable<MarketStall> stalls)
        {
            ArgumentNullException.ThrowIfNull(stalls);
            if (columns < MinSize || columns > MaxColumns)
                throw new DomainException($"A rács szélessége {MinSize} és {MaxColumns} cella között lehet.");
            if (rows < MinSize || rows > MaxRows)
                throw new DomainException($"A rács magassága {MinSize} és {MaxRows} cella között lehet.");

            var list = stalls
                .Select(s => s with { Label = string.IsNullOrWhiteSpace(s.Label) ? null : s.Label.Trim() })
                .OrderBy(s => s.Number)
                .ToList();
            if (list.Count > MaxStalls)
                throw new DomainException($"Egy piacon legfeljebb {MaxStalls} stand lehet.");

            foreach (var s in list)
            {
                if (s.Number < 1 || s.Number > MaxStallNumber)
                    throw new DomainException($"A standszám 1 és {MaxStallNumber} között lehet (kapott: {s.Number}).");
                if (s.Width < 1 || s.Height < 1 || s.Width > MaxStallSize || s.Height > MaxStallSize)
                    throw new DomainException($"A(z) {s.Number}. stand mérete 1 és {MaxStallSize} cella között lehet.");
                if (s.X < 0 || s.Y < 0 || s.X + s.Width > columns || s.Y + s.Height > rows)
                    throw new DomainException($"A(z) {s.Number}. stand kilóg a piac területéről.");
                if (s.Label is { Length: > MaxLabelLength })
                    throw new DomainException($"A(z) {s.Number}. stand felirata legfeljebb {MaxLabelLength} karakter lehet.");
            }

            var duplicate = list.GroupBy(s => s.Number).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                throw new DomainException($"A(z) {duplicate.Key}. standszám többször szerepel.");

            for (var i = 0; i < list.Count; i++)
                for (var j = i + 1; j < list.Count; j++)
                    if (Overlap(list[i], list[j]))
                        throw new DomainException($"A(z) {list[i].Number}. és a(z) {list[j].Number}. stand átfedi egymást.");

            return new MarketLayout(columns, rows, list);
        }

        public bool HasStall(int number) => Stalls.Any(s => s.Number == number);

        private static bool Overlap(MarketStall a, MarketStall b) =>
            a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
    }
}
