using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Domain.Entities;
using OrderWebAPI.Domain.Enums;
using OrderWebAPI.Infrastructure.Persistence;

namespace OrderWebAPI.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(
        Guid? customerId, string? status, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _context.Orders.Include(o => o.Items).AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);
    
        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }
}