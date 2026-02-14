using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Entities;

public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserRoleType RoleName { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
