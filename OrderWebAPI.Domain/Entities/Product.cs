using System;
using System.Collections.Generic;
using System.Text;

namespace OrderWebAPI.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public decimal UnitPrice { get; private set; }
        public int AvailableQuantity { get; private set; }

        private Product() { }

        public Product(string name, decimal unitPrice, int availableQuantity)
        {
            Id = Guid.NewGuid();
            Name = name;
            UnitPrice = unitPrice;
            AvailableQuantity = availableQuantity;
        }

        public void DecreaseStock(int quantity)
        {
            if (quantity > AvailableQuantity)
                throw new InvalidOperationException($"Estoque insuficiente para o produto {Name}.");
            AvailableQuantity -= quantity;
        }

        public void IncreaseStock(int quantity)
        {
            AvailableQuantity += quantity;
        }
    }
}
