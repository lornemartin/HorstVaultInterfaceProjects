namespace HorstMFG.VaultGateway;

public class GatewayConfig
{
    public string ListenUrl { get; set; } = "http://127.0.0.1:5050";
    public string CallbackApiKey { get; set; } = "";
}

public class VaultConfig
{
    public string Server   { get; set; } = "";
    public string Vault    { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
