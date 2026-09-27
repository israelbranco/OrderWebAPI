using OrderWebAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderWebAPI.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids);
        Task AddAsync(Product product);
    }
}
