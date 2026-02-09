namespace SyncState.Sample.Domain;

public class OrderItem : Entity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    
    public Order? Order { get; set; }
}

