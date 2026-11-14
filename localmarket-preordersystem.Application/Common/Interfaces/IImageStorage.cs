namespace localmarket_preordersystem.Application.Common.Interfaces;

/// <summary>Feltöltött képek tárolása (Infrastructure implementálja: jelenleg helyi fájlrendszer).</summary>
public interface IImageStorage
{
    /// <summary>Elmenti a képet és visszaadja a generált, egyedi fájlnevet (a megadott kiterjesztéssel).</summary>
    Task<string> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Törli a fájlt; ha nem létezik, nem hiba.</summary>
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
