using OrderWebAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderWebAPI.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public string Currency { get; private set; }
        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
        public decimal Total { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Order() { }

        public Order(Guid customerId, string currency, List<OrderItem> items)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("O pedido deve conter pelo menos um item.", nameof(items));

            Id = Guid.NewGuid();
            CustomerId = customerId;
            Currency = currency;
            Status = OrderStatus.Placed; // Conforme requisito, nasce como Placed
            CreatedAt = DateTime.UtcNow;

            _items.AddRange(items);
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            Total = _items.Sum(i => i.UnitPrice * i.Quantity);
        }

        public void Confirm()
        {
            if (Status != OrderStatus.Placed)
                throw new InvalidOperationException("Apenas pedidos com status 'Placed' podem ser confirmados.");

            Status = OrderStatus.Confirmed;
        }

        public void Cancel()
        {
            if (Status != OrderStatus.Placed && Status != OrderStatus.Confirmed)
                throw new InvalidOperationException("Apenas pedidos 'Placed' ou 'Confirmed' podem ser cancelados.");

            Status = OrderStatus.Canceled;
        }
    }
}
