using OrderWebAPI.Application.DTOs;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Domain.Entities;
using OrderWebAPI.Domain.Enums;

namespace OrderWebAPI.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            throw new ArgumentException("O pedido deve conter ao menos um item.");

        var productIds = dto.Items.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(productIds);

        var orderItems = new List<OrderItem>();

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
                throw new ArgumentException("A quantidade do item deve ser maior que zero.");

            var product = products.FirstOrDefault(p => p.Id == itemDto.ProductId)
                          ?? throw new InvalidOperationException($"Produto {itemDto.ProductId} não encontrado.");

            if (product.AvailableQuantity < itemDto.Quantity)
                throw new InvalidOperationException($"Estoque insuficiente para o produto {product.Name}. Disponível: {product.AvailableQuantity}");

            orderItems.Add(new OrderItem(product.Id, product.UnitPrice, itemDto.Quantity));
        }

        var order = new Order(dto.CustomerId, dto.Currency, orderItems);
        await _orderRepository.AddAsync(order);

        return MapToDto(order);
    }

    public async Task ConfirmOrderAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("Pedido não encontrado.");

        // Idempotência: Se já estiver confirmado, apenas retorna com sucesso
        if (order.Status == OrderStatus.Confirmed)
            return;

        if (order.Status != OrderStatus.Placed)
            throw new InvalidOperationException("Apenas pedidos com status 'Placed' podem ser confirmados.");

        // Baixar estoque dos produtos
        var productIds = order.Items.Select(i => i.ProductId);
        var products = await _productRepository.GetByIdsAsync(productIds);

        foreach (var item in order.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.DecreaseStock(item.Quantity);
        }

        order.Confirm();
        await _orderRepository.UpdateAsync(order);
    }

    public async Task CancelOrderAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("Pedido não encontrado.");

        // Idempotência: Se já estiver cancelado, retorna com sucesso
        if (order.Status == OrderStatus.Canceled)
            return;

        // Se o pedido estava Confirmado, precisamos devolver o estoque
        if (order.Status == OrderStatus.Confirmed)
        {
            var productIds = order.Items.Select(i => i.ProductId);
            var products = await _productRepository.GetByIdsAsync(productIds);

            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.IncreaseStock(item.Quantity);
            }
        }

        order.Cancel();
        await _orderRepository.UpdateAsync(order);
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<(IEnumerable<OrderResponseDto> Orders, int TotalCount)> GetPagedAsync(
        Guid? customerId, string? status, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var (orders, totalCount) = await _orderRepository.GetPagedAsync(customerId, status, from, to, page, pageSize);
        return (orders.Select(MapToDto), totalCount);
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        return new OrderResponseDto(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Currency,
            order.Total,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemResponseDto(i.ProductId, i.UnitPrice, i.Quantity)).ToList()
        );
    }
}