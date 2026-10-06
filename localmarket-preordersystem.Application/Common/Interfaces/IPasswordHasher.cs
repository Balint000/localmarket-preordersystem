namespace localmarket_preordersystem.Application.Common.Interfaces
{
    /// <summary>
    /// A Domain (User.Register) szándékosan nem tud semmit a konkrét hashing algoritmusról —
    /// csak egy kész hash-t vár. Ez az interfész adja a hashelést/ellenőrzést, Infrastructure
    /// implementálja (pl. ASP.NET Core Identity PasswordHasher vagy BCrypt).
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string passwordHash);
    }
}