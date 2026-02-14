namespace HorstMFG.Core.Entities;

public class RadanIdAssignment
{
    public int Id { get; set; }
    public int RadanIdNumber { get; set; }
    public int OrderItemId { get; set; }
    public int PlantId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public bool IsReleased { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
}
