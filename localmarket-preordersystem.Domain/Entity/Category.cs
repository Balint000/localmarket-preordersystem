using localmarket_preordersystem.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.Entity
{
    public class Category
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        private Category()
        {
            // EF Core-nak kell
        }

        public static Category Create(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A kategória nevének megadása kötelező.");

            return new Category
            {
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty
            };
        }
    }
}
