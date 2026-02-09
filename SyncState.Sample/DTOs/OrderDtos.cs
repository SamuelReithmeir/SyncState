using SyncState.Sample.Domain;

namespace SyncState.Sample.DTOs;

public record CreateOrderDto(
    int CustomerId,
    string Street,
    string City,
    string PostalCode,
    string Country
);

public record UpdateOrderDto(
    OrderStatus Status,
    string Street,
    string City,
    string PostalCode,
    string Country
);

public record OrderItemDto(
    int Id,
    int ProductId,
    int Quantity,
    decimal Price
);

public record CreateOrderItemDto(
    int ProductId,
    int Quantity,
    decimal Price
);

public record UpdateOrderItemDto(
    int Quantity,
    decimal Price
);

public record OrderDto(
    int Id,
    int CustomerId,
    DateTime OrderDate,
    OrderStatus Status,
    string Street,
    string City,
    string PostalCode,
    string Country,
    ICollection<OrderItemDto> OrderItems
);

