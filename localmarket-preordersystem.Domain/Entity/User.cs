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
    }
}
