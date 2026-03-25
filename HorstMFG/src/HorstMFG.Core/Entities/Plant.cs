namespace HorstMFG.Core.Entities;

public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<NestOrder> NestOrders { get; set; } = new List<NestOrder>();
    public ICollection<NestBatch> NestBatches { get; set; } = new List<NestBatch>();
    public ICollection<Nest> Nests { get; set; } = new List<Nest>();
    public ICollection<RadanIdAssignment> RadanIdAssignments { get; set; } = new List<RadanIdAssignment>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
