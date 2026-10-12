namespace localmarket_preordersystem.Application.Common.Interfaces
{
    /// <summary>
    /// Ez az EGYETLEN "UnitOfWork-szerű" absztrakció, amit tudatosan bevezetünk — az
    /// eredeti irányelv ("ne vezess be UnitOfWork abstractiont EF Core fölé csak azért,
    /// mert népszerű") itt sem szűnik meg, de van rá konkrét indok: a repository-k
    /// aggregate-enként vannak felosztva (IUserRepository, IProducerRepository, ...), és
    /// szükség van EGY közös commit-pontra egy use case végén, anélkül hogy az Application
    /// réteg közvetlenül az EF Core DbContext típusától függne. Nem generikus, nem
    /// tartalmaz Begin/Commit/Rollback tranzakció-kezelést — csak ez az egy metódus.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}