using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class User
    {
        public int Id { get; private set; }
        public int ProducerId { get; private set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Customer;
        public Producer? Producer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        private User()
        {
            // EF Core-nak kell
        }

        /// <summary>
        /// Regisztráció. A jelszó hash-elése Application/Infrastructure feladat (pl. egy
        /// IPasswordHasher interfészen keresztül), a Domain csak a kész hash-t tárolja,
        /// nem függhet konkrét hashing könyvtártól.
        /// </summary>
        public static User Register(string firstName, string lastName, string email, string passwordHash, Role role, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("A keresztnév megadása kötelező.");
            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("A vezetéknév megadása kötelező.");
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Az e-mail cím megadása kötelező.");
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("A jelszó megadása kötelező.");

            return new User
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = role,
                PhoneNumber = phoneNumber?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>Producer szerepkörű felhasználóhoz köti a saját árus-profilját (regisztráció után).</summary>
        public void AttachProducerProfile(Producer producer)
        {
            if (Role != Role.Producer)
                throw new DomainException("Csak Producer szerepkörű felhasználóhoz köthető árus-profil.");
            ArgumentNullException.ThrowIfNull(producer);
            if (Producer is not null)
                throw new DomainException("Ehhez a felhasználóhoz már tartozik árus-profil.");

            Producer = producer;
        }

        public void ChangePasswordHash(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new DomainException("A jelszó megadása kötelező.");

            PasswordHash = newPasswordHash;
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;
    }
}
