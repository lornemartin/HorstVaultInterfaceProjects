using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface IPlantService
{
    Task<IEnumerable<Plant>> GetPlantsAsync();
    Task<Plant?> GetPlantByIdAsync(int id);
    Task<Plant> CreatePlantAsync(Plant plant);
    Task UpdatePlantAsync(Plant plant);
    Task<Plant?> GetUserPlantAsync(int userId);
}
