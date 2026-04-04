namespace HorstMFG.Core.Entities;

public class ApplicationUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int  PlantId   { get; set; }
    public int? StationId { get; set; }
    public bool IsActive  { get; set; } = true;

    public Plant Plant { get; set; } = null!;
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}
