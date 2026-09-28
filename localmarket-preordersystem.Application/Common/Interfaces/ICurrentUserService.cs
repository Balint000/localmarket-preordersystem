using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        int? UserId { get; }
        Role? Role { get; }

        /// <summary>Csak Producer szerepkörnél </summary>
        int? ProducerId { get; }
    }
}
