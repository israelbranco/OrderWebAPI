using OrderWebAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderWebAPI.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(Guid? customerId, string? status, DateTime? from, DateTime? to, int page, int pageSize);
    }
}
