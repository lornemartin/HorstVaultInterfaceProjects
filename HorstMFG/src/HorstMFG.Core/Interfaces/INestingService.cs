using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface INestingService
{
    Task<IEnumerable<Nest>> GetNestsAsync(int? plantId = null);
    Task<Nest?> GetNestByIdAsync(int id);
    Task<Nest> CreateNestAsync(Nest nest);
    Task UpdateNestAsync(Nest nest);
    Task AddNestedPartAsync(int nestId, int orderItemId, int qty);
}
