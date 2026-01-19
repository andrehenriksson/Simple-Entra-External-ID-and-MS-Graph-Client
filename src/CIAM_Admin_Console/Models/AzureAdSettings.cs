namespace CIAM_Admin_Console.Models;

public class AzureAdSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}
