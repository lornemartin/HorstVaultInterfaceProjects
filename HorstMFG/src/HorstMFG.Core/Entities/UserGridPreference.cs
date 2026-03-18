namespace HorstMFG.Core.Entities;

public class UserGridPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GridId { get; set; } = string.Empty;
    public string CollapsedState { get; set; } = "[]";

    public ApplicationUser User { get; set; } = null!;
}
