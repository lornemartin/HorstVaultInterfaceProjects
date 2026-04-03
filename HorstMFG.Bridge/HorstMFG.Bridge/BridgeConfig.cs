namespace HorstMFG.Bridge;

public class BridgeConfig
{
    public int    StationId             { get; set; }
    public string NestingSoftware       { get; set; } = "Radan";
    public string HorstMfgUrl           { get; set; } = "";
    public string ApiKey                { get; set; } = "";
    public string RadanProjectsRootPath { get; set; } = "";
    public string SymNetworkSharePath   { get; set; } = "";
    public string BomExportFilePath     { get; set; } = "";
}

public class VaultConfig
{
    public string Server   { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Vault    { get; set; } = "";
}
