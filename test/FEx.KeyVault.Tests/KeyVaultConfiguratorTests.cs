using Microsoft.Extensions.Configuration;
using Shouldly;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace FEx.KeyVault.Tests;

/// <summary>
/// Wiring Key Vault into a configuration builder. Nothing here talks to Azure: the sources are only added, never
/// built or loaded, and the credentials are constructed lazily by the Azure SDK.
/// </summary>
[Collection(CertificateStoreCollection.Name)]
public sealed class KeyVaultConfiguratorTests
{
    private static KeyVaultCredentials Credentials() =>
        new()
        {
            KeyVaultName = "my-vault",
            AzureADTenantId = "00000000-0000-0000-0000-000000000001",
            AzureADClientId = "00000000-0000-0000-0000-000000000002",
            AzureADClientSecret = "not-a-real-secret",
            AzureADCertThumbprint = "0000000000000000000000000000000000000000"
        };

    #region client secret

    [Fact]
    public void AddAzureKeyVaultWithClientSecret_AddsExactlyOneConfigurationSource()
    {
        ConfigurationBuilder builder = new();

        builder.AddAzureKeyVaultWithClientSecret(Credentials());

        builder.Sources.ShouldHaveSingleItem().GetType().Name.ShouldContain("KeyVault");
    }

    [Fact]
    public void AddAzureKeyVaultWithClientSecret_AddsTheSourceAfterExistingOnes()
    {
        ConfigurationBuilder builder = new();
        builder.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?> { ["a"] = "b" });

        builder.AddAzureKeyVaultWithClientSecret(Credentials());

        builder.Sources.Count.ShouldBe(2);
        builder.Sources[1].GetType().Name.ShouldContain("KeyVault");
    }

    [Fact]
    public void AddAzureKeyVaultWithClientSecret_WithoutCredentials_Throws()
    {
        ConfigurationBuilder builder = new();

        Should.Throw<ArgumentNullException>(() => builder.AddAzureKeyVaultWithClientSecret(null!))
            .ParamName.ShouldBe("credentials");
        builder.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void AddAzureKeyVaultWithClientSecret_WithoutAVaultName_Throws()
    {
        ConfigurationBuilder builder = new();
        var credentials = Credentials();
        credentials.KeyVaultName = null;

        Should.Throw<ArgumentNullException>(() => builder.AddAzureKeyVaultWithClientSecret(credentials))
            .ParamName.ShouldBe("vaultName");
        builder.Sources.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("client")]
    [InlineData("secret")]
    public void AddAzureKeyVaultWithClientSecret_WithAMissingCredentialValue_Throws(string missing)
    {
        ConfigurationBuilder builder = new();
        var credentials = Credentials();

        switch (missing)
        {
            case "tenant":
                credentials.AzureADTenantId = null;

                break;
            case "client":
                credentials.AzureADClientId = null;

                break;
            default:
                credentials.AzureADClientSecret = null;

                break;
        }

        Should.Throw<ArgumentNullException>(() => builder.AddAzureKeyVaultWithClientSecret(credentials));
        builder.Sources.ShouldBeEmpty();
    }

    #endregion

    #region certificate

    [Fact]
    public void AddAzureKeyVaultWithCertificate_WithoutCredentials_Throws()
    {
        ConfigurationBuilder builder = new();

        Should.Throw<ArgumentNullException>(() => builder.AddAzureKeyVaultWithCertificate(null!))
            .ParamName.ShouldBe("credentials");
    }

    [Fact]
    public void AddAzureKeyVaultWithCertificate_WithoutAVaultName_Throws()
    {
        ConfigurationBuilder builder = new();
        var credentials = Credentials();
        credentials.KeyVaultName = null;

        Should.Throw<ArgumentNullException>(() => builder.AddAzureKeyVaultWithCertificate(credentials))
            .ParamName.ShouldBe("vaultName");
        builder.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void AddAzureKeyVaultWithCertificate_WithoutAThumbprint_Throws()
    {
        ConfigurationBuilder builder = new();
        var credentials = Credentials();
        credentials.AzureADCertThumbprint = null;

        Should.Throw<ArgumentNullException>(() => builder.AddAzureKeyVaultWithCertificate(credentials))
            .ParamName.ShouldBe("certificateThumbprint");
        builder.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void AddAzureKeyVaultWithCertificate_WhenTheCertificateIsNotInTheStore_Throws()
    {
        ConfigurationBuilder builder = new();

        Should.Throw<InvalidOperationException>(() => builder.AddAzureKeyVaultWithCertificate(Credentials()));
        builder.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void AddAzureKeyVaultWithCertificate_WhenTheCertificateIsInTheStore_AddsOneConfigurationSource()
    {
        using var certificate = CreateSelfSignedCertificate();
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Add(certificate);

        try
        {
            ConfigurationBuilder builder = new();
            var credentials = Credentials();
            credentials.AzureADCertThumbprint = certificate.Thumbprint;

            builder.AddAzureKeyVaultWithCertificate(credentials);

            builder.Sources.ShouldHaveSingleItem().GetType().Name.ShouldContain("KeyVault");
        }
        finally
        {
            store.Remove(certificate);
        }
    }

    [Fact]
    public void AddAzureKeyVaultWithCertificate_AfterTheCertificateIsRemoved_ThrowsAgain()
    {
        using var certificate = CreateSelfSignedCertificate();
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Add(certificate);
        store.Remove(certificate);
        var credentials = Credentials();
        credentials.AzureADCertThumbprint = certificate.Thumbprint;

        Should.Throw<InvalidOperationException>(() => new ConfigurationBuilder().AddAzureKeyVaultWithCertificate(credentials));
    }

    #endregion

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        CertificateRequest request = new($"CN=fex-keyvault-test-{Guid.NewGuid():N}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // Round-trip through PFX so the private key is exportable/persistable on every platform.
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pfx, "x"), "x", X509KeyStorageFlags.PersistKeySet);
    }
}
