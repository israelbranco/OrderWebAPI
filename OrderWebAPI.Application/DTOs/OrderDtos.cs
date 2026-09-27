namespace OrderWebAPI.Application.DTOs;

public record CreateOrderItemDto(Guid ProductId, int Quantity);

public record CreateOrderDto(Guid CustomerId, string Currency, List<CreateOrderItemDto> Items);

public record OrderItemResponseDto(Guid ProductId, decimal UnitPrice, int Quantity);

public record OrderResponseDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    DateTime CreatedAt,
    List<OrderItemResponseDto> Items
);