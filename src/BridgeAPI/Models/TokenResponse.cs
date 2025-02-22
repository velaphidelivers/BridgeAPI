using Newtonsoft.Json;

namespace Models;
public class TokenResponse
{
    [JsonProperty("systemSecurityToken")]
    public SystemSecurityToken? SystemSecurityToken { get; set; }
}

public class SystemSecurityToken
{
    [JsonProperty("token")]
    public string? Token { get; set; }

    [JsonProperty("expiryInSeconds")]
    public int ExpiryInSeconds { get; set; }
}
