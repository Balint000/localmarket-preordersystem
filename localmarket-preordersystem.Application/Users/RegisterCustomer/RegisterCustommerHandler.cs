using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Users.RegisterCustomer
{
    /// <summary>
    /// Vásárló regisztrációja. Nincs jóváhagyási folyamata (szemben a Producer
    /// regisztrációval) — a Customer azonnal használható fiókkal jön létre.
    /// </summary>
    public sealed class RegisterCustomerHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCustomerHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
        {
            RegisterCustomerValidator.Validate(command);

            if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
                throw new ConflictException("Ez az e-mail cím már regisztrálva van.");

            var passwordHash = _passwordHasher.Hash(command.Password);
            var user = User.Register(
                command.FirstName,
                command.LastName,
                command.Email,
                passwordHash,
                Role.Customer,
                command.PhoneNumber);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}