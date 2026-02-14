using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface IPartService
{
    Task<IEnumerable<Part>> GetPartsAsync(string? searchTerm = null);
    Task<Part?> GetPartByIdAsync(int id);
    Task<Part?> GetPartByNumberAsync(string number);
    Task<Part> CreatePartAsync(Part part);
    Task UpdatePartAsync(Part part);
}
