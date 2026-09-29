using OrderWebAPI.Application.DTOs;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Domain.Entities;
using OrderWebAPI.Domain.Enums;
using System.Text.RegularExpressions;

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
        // Validação de Currency
        if (string.IsNullOrWhiteSpace(dto.Currency) || !Regex.IsMatch(dto.Currency, @"^[A-Z]{3}$"))
            throw new Application.Exceptions.ValidationException(new[] { new Application.Exceptions.ValidationError("Currency", "A moeda (currency) deve ser um código válido com 3 letras maiúsculas (ex: BRL, USD, EUR).") });

        if (dto.Items == null || !dto.Items.Any())
            throw new Application.Exceptions.ValidationException(new[] { new Application.Exceptions.ValidationError("Items", "O pedido deve conter ao menos um item.") });

        // validações básicas por item
        var perItemErrors = new List<Application.Exceptions.ValidationError>();
        foreach (var it in dto.Items)
        {
            if (it.Quantity <= 0)
                perItemErrors.Add(new Application.Exceptions.ValidationError($"Items[{it.ProductId}]", "A quantidade do item deve ser maior que zero.", "InvalidQuantity", new { ProductId = it.ProductId, Quantity = it.Quantity }));
        }

        if (perItemErrors.Any())
            throw new Application.Exceptions.ValidationException(perItemErrors);

        var productIds = dto.Items.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(productIds);

        // Agregar quantidades por produto para validar estoque corretamente
        var aggregated = dto.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // Validar existência e disponibilidade considerando a soma das quantidades
        var stockErrors = new List<Application.Exceptions.ValidationError>();
        var notFoundErrors = new List<Application.Exceptions.ValidationError>();

        foreach (var kvp in aggregated)
        {
            var product = products.FirstOrDefault(p => p.Id == kvp.Key);
            if (product == null)
            {
                notFoundErrors.Add(new Application.Exceptions.ValidationError($"Items[{kvp.Key}]", "Produto não encontrado.", "ProductNotFound", new { ProductId = kvp.Key }));
                continue;
            }

            if (product.AvailableQuantity < kvp.Value)
            {
                stockErrors.Add(new Application.Exceptions.ValidationError($"Items[{kvp.Key}]", "Estoque insuficiente.", "InsufficientStock", new { ProductId = kvp.Key, Available = product.AvailableQuantity, Requested = kvp.Value }));
            }
        }

        if (notFoundErrors.Any() || stockErrors.Any())
            throw new Application.Exceptions.ValidationException(notFoundErrors.Concat(stockErrors));

        var orderItems = aggregated.Select(kvp =>
            {
                var product = products.First(p => p.Id == kvp.Key);
                return new OrderItem(product.Id, product.UnitPrice, kvp.Value);
            })
            .ToList();

        var order = new Order(dto.CustomerId, dto.Currency, orderItems);
        await _orderRepository.AddAsync(order);

        return MapToDto(order);
    }

    public async Task ConfirmOrderAsync(Guid id)
    {
        const int maxRetries = 3;
        var attempt = 0;

        while (true)
        {
            attempt++;

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

            // Verificar disponibilidade antes de aplicar alterações
            var insufficient = new List<Application.Exceptions.ValidationError>();
            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId)
                              ?? throw new InvalidOperationException($"Produto {item.ProductId} não encontrado.");

                if (product.AvailableQuantity < item.Quantity)
                    insufficient.Add(new Application.Exceptions.ValidationError($"Items[{item.ProductId}]", "Estoque insuficiente no momento da confirmação.", "InsufficientStock", new { ProductId = item.ProductId, Available = product.AvailableQuantity, Requested = item.Quantity }));
            }

            if (insufficient.Any())
                throw new Application.Exceptions.ValidationException(insufficient);

            // Aplicar alterações na memória
            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.DecreaseStock(item.Quantity);
            }

            order.Confirm();

            try
            {
                await _orderRepository.UpdateAsync(order);
                return; // sucesso
            }
            catch (Exception ex)
            {
                // Mapear exceção de concorrência para um tipo de domínio caso venha do EF Core
                if (ex.GetType().Name == "DbUpdateConcurrencyException")
                {
                    if (attempt >= maxRetries)
                        throw new Application.Exceptions.ConcurrencyException("Falha ao confirmar pedido devido a conflito de concorrência. Tente novamente.");

                    await Task.Delay(100 * attempt);
                    continue;
                }

                throw; // rethrow para outros tipos de erro
            }
        }
    }

    public async Task CancelOrderAsync(Guid id)
    {
        const int maxRetries = 3;
        var attempt = 0;

        while (true)
        {
            attempt++;

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

                // Aplicar devolução de estoque
                foreach (var item in order.Items)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId)
                                  ?? throw new InvalidOperationException($"Produto {item.ProductId} não encontrado.");
                    product.IncreaseStock(item.Quantity);
                }
            }

            order.Cancel();

            try
            {
                await _orderRepository.UpdateAsync(order);
                return;
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "DbUpdateConcurrencyException")
                {
                    if (attempt >= maxRetries)
                        throw new Application.Exceptions.ConcurrencyException("Falha ao cancelar pedido devido a conflito de concorrência. Tente novamente.");

                    await Task.Delay(100 * attempt);
                    continue;
                }

                throw;
            }
        }
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