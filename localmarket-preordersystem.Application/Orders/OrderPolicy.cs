namespace localmarket_preordersystem.Application.Orders
{
    /// <summary>
    /// A rendelési folyamat konfigurálható szabályai (API: Orders:* beállítások).
    /// </summary>
    /// <param name="CustomerCancellationCutoff">
    /// Az elfogadott rendelést a vevő az átvételi idősáv kezdete előtt legfeljebb ennyivel mondhatja le
    /// (utána az árus már összekészítette). A még el nem fogadott rendelés az idősáv kezdetéig lemondható.
    /// </param>
    public sealed record OrderPolicy(TimeSpan CustomerCancellationCutoff)
    {
        public static OrderPolicy Default { get; } = new(TimeSpan.FromHours(12));
    }
}
