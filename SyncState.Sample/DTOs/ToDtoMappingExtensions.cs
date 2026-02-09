using SyncState.Sample.Domain;

namespace SyncState.Sample.DTOs;

public static class ToDtoMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new OrderDto(
        order.Id, order.CustomerId, order.OrderDate, order.Status, order.Street, order.City, order.PostalCode,
        order.Country, order.OrderItems.Select(oi => oi.ToDto()).ToList());

    public static OrderItemDto ToDto(this OrderItem orderItem) =>
        new OrderItemDto(orderItem.Id, orderItem.ProductId, orderItem.Quantity, orderItem.Price);
}