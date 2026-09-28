using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderWebAPI.Domain.Entities;

namespace OrderWebAPI.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Products.AnyAsync())
                return; // já possui dados

            // Criar 3 produtos
            var p1 = new Product("Produto A", 10.50m, 100);
            var p2 = new Product("Produto B", 20.00m, 100);
            var p3 = new Product("Produto C", 30.00m, 100);

            db.Products.AddRange(p1, p2, p3);
            await db.SaveChangesAsync();

            var products = new[] { p1, p2, p3 };

            // Criar 3 pedidos, cada um com 3 itens
            var orders = new List<Order>();

            for (int o = 0; o < 3; o++)
            {
                var items = new List<OrderItem>();

                // cada pedido terá 3 itens (um de cada produto) com quantidades variadas
                items.Add(new OrderItem(p1.Id, p1.UnitPrice, 1 + o));
                items.Add(new OrderItem(p2.Id, p2.UnitPrice, 1 + o));
                items.Add(new OrderItem(p3.Id, p3.UnitPrice, 1 + o));

                var order = new Order(Guid.NewGuid(), "USD", items);
                orders.Add(order);
            }

            db.Orders.AddRange(orders);

            // Ajustar estoque dos produtos conforme os pedidos
            var totalQtyP1 = orders.Sum(x => x.Items.Count(i => i.ProductId == p1.Id) == 0 ? 0 : orders.Sum(ord => ord.Items.Where(it => it.ProductId == p1.Id).Sum(it => it.Quantity)));
            // Simplificar: calcular por produto
            var qtyP1 = orders.SelectMany(ord => ord.Items).Where(it => it.ProductId == p1.Id).Sum(it => it.Quantity);
            var qtyP2 = orders.SelectMany(ord => ord.Items).Where(it => it.ProductId == p2.Id).Sum(it => it.Quantity);
            var qtyP3 = orders.SelectMany(ord => ord.Items).Where(it => it.ProductId == p3.Id).Sum(it => it.Quantity);

            // Decrementar estoque (método DecreaseStock valida quantidade)
            p1.DecreaseStock(qtyP1);
            p2.DecreaseStock(qtyP2);
            p3.DecreaseStock(qtyP3);

            await db.SaveChangesAsync();
        }
    }
}
