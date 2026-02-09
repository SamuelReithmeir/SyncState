using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncState.Sample.Domain;
using SyncState.Sample.DTOs;
using SyncState.Sample.Infrastructure;

namespace SyncState.Sample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly SampleDbContext _context;

    public OrdersController(SampleDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all orders with their items
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        return await _context.Orders.Include(o => o.OrderItems).ToListAsync();
    }

    /// <summary>
    /// Get a specific order by id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return order;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto dto)
    {
        var order = new Order
        {
            CustomerId = dto.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Street = dto.Street,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Country = dto.Country
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDto dto)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        order.Status = dto.Status;
        order.Street = dto.Street;
        order.City = dto.City;
        order.PostalCode = dto.PostalCode;
        order.Country = dto.Country;

        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete an order and its items
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Add an item to an order
    /// </summary>
    [HttpPost("{orderId}/items")]
    public async Task<ActionResult<OrderItem>> AddOrderItem(int orderId, CreateOrderItemDto dto)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
        {
            return NotFound("Order not found");
        }

        var item = new OrderItem
        {
            OrderId = orderId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Price = dto.Price
        };

        _context.OrderItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, item);
    }

    /// <summary>
    /// Update an order item
    /// </summary>
    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateOrderItem(int itemId, UpdateOrderItemDto dto)
    {
        var item = await _context.OrderItems.FindAsync(itemId);

        if (item == null)
        {
            return NotFound();
        }

        item.Quantity = dto.Quantity;
        item.Price = dto.Price;

        _context.OrderItems.Update(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete an order item
    /// </summary>
    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> DeleteOrderItem(int itemId)
    {
        var item = await _context.OrderItems.FindAsync(itemId);

        if (item == null)
        {
            return NotFound();
        }

        _context.OrderItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

