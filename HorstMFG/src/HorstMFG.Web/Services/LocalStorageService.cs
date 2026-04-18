using System.Text.Json;
using Microsoft.JSInterop;

namespace HorstMFG.Web.Services;

public class LocalStorageService(IJSRuntime js)
{
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
            return json is null ? default : JsonSerializer.Deserialize<T>(json, _opts);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _opts);
            await js.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch { }
    }

    public async Task RemoveAsync(string key)
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", key); }
        catch { }
    }
}
