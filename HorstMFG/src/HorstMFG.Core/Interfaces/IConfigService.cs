namespace HorstMFG.Core.Interfaces;

public interface IConfigService
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, string? description = null);
    Task<IDictionary<string, string>> GetAllAsync();
}
