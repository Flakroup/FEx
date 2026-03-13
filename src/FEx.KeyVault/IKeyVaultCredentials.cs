namespace FEx.KeyVault;

public interface IKeyVaultCredentials
{
    string AzureADTenantId { get; }
    string AzureADClientId { get; }
    string KeyVaultName { get; }
}

public interface IKeyVaultByCertCredentials : IKeyVaultCredentials
{
    string AzureADCertThumbprint { get; }
}

public interface IKeyVaultByClientSecretCredentials : IKeyVaultCredentials
{
    string AzureADClientSecret { get; }
}