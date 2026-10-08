using Shouldly;
using Xunit;

namespace FEx.KeyVault.Tests;

/// <summary>The credentials bag serves both authentication flavours from one object.</summary>
public sealed class KeyVaultCredentialsTests
{
    [Fact]
    public void NewCredentials_HaveNothingSet()
    {
        KeyVaultCredentials credentials = new();

        credentials.KeyVaultName.ShouldBeNull();
        credentials.AzureADTenantId.ShouldBeNull();
        credentials.AzureADClientId.ShouldBeNull();
        credentials.AzureADCertThumbprint.ShouldBeNull();
        credentials.AzureADClientSecret.ShouldBeNull();
    }

    [Fact]
    public void EveryValue_IsVisibleThroughBothCredentialInterfaces()
    {
        KeyVaultCredentials credentials = new()
        {
            KeyVaultName = "vault",
            AzureADTenantId = "tenant",
            AzureADClientId = "client",
            AzureADCertThumbprint = "thumb",
            AzureADClientSecret = "secret"
        };

        IKeyVaultByCertCredentials byCert = credentials;
        IKeyVaultByClientSecretCredentials bySecret = credentials;
        IKeyVaultCredentials common = credentials;

        common.KeyVaultName.ShouldBe("vault");
        common.AzureADTenantId.ShouldBe("tenant");
        common.AzureADClientId.ShouldBe("client");
        byCert.AzureADCertThumbprint.ShouldBe("thumb");
        bySecret.AzureADClientSecret.ShouldBe("secret");
    }
}
