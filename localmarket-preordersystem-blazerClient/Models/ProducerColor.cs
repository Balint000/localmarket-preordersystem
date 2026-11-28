namespace localmarket_preordersystem.Models;

/// <summary>
/// Árusonként állandó szín a piactérképen és a jelmagyarázatban (az árus azonosítójából, így minden oldalon
/// ugyanaz). A színeket a design.css .pc-0 … .pc-7 osztályai adják, világos és sötét témára is.
/// </summary>
public static class ProducerColor
{
    public const int Count = 8;

    public static string Css(int producerId) => $"pc-{((producerId % Count) + Count) % Count}";
}
