namespace SyncState.Sample.Domain;

public class Order : Entity
{
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

