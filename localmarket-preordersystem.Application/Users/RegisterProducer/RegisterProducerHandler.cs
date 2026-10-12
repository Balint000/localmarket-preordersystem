using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Users.RegisterProducer
{
    /// <summary>
    /// Árus regisztrációja. A létrejövő Producer.Status alapból Pending — a piacvezető
    /// jóváhagyása (Producer.Approve(), külön use case) nélkül nem hozhat létre terméket
    /// (ld. Product.Create invariáns). Ez a use case tehát NEM ad azonnali listázási jogot.
    /// </summary>
    public sealed class RegisterProducerHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IProducerRepository _producerRepository;
        private readonly IMarketRepository _marketRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterProducerHandler(
            IUserRepository userRepository,
            IProducerRepository producerRepository,
            IMarketRepository marketRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _producerRepository = producerRepository;
            _marketRepository = marketRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> HandleAsync(RegisterProducerCommand command, CancellationToken cancellationToken = default)
        {
            RegisterProducerValidator.Validate(command);

            if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
                throw new ConflictException("Ez az e-mail cím már regisztrálva van.");

            var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken)
                ?? throw new NotFoundException($"Nem található piac ezzel az azonosítóval: {command.MarketId}.");

            var passwordHash = _passwordHasher.Hash(command.Password);
            var user = User.Register(
                command.FirstName,
                command.LastName,
                command.Email,
                passwordHash,
                Role.Producer,
                command.PhoneNumber);

            var producer = Producer.Register(market, command.ProducerName, command.StallNumber);
            user.AttachProducerProfile(producer);

            await _userRepository.AddAsync(user, cancellationToken);
            await _producerRepository.AddAsync(producer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}