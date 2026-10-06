using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Auth.Login
{
    /// <summary>
    /// Bejelentkezés. Tudatosan UGYANAZT az üzenetet dobja "nincs ilyen e-mail" és
    /// "hibás jelszó" esetén is (AuthenticationException) — így a hibaüzenetből nem
    /// deríthető ki, hogy egy adott e-mail cím regisztrálva van-e a rendszerben.
    /// </summary>
    public sealed class LoginHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public LoginHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<AuthResultDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            LoginValidator.Validate(command);

            var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

            if (user is null || !user.IsActive || !_passwordHasher.Verify(command.Password, user.PasswordHash))
                throw new AuthenticationException("Hibás e-mail cím vagy jelszó.");

            var token = _tokenService.GenerateToken(user);

            return new AuthResultDto(
                token.Value,
                token.ExpiresAtUtc,
                user.Id,
                user.Role.ToString(),
                user.Producer?.Id);
        }
    }
}