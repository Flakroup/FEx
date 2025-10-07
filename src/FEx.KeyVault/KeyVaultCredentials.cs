namespace FEx.KeyVault;

public class KeyVaultCredentials : IKeyVaultByCertCredentials, IKeyVaultByClientSecretCredentials
{
    public string KeyVaultName { get; set; }

    /// <summary>
    /// Gets or sets the Directory (tenant) ID
    /// </summary>
    public string AzureADTenantId { get; set; }

    /// <summary>
    /// Gets or sets the Application (client) ID
    /// </summary>
    public string AzureADClientId { get; set; }

    public string AzureADCertThumbprint { get; set; }
    public string AzureADClientSecret { get; set; }
}