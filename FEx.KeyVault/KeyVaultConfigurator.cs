using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace FEx.KeyVault
{
    public static class KeyVaultConfigurator
    {
        public static void AddAzureKeyVaultWithCertificate(this IConfigurationBuilder config, IKeyVaultByCertCredentials credentials)
        {
            Uri keyVaultEndpoint = GetKeyVaultEndpoint(credentials.KeyVaultName);
            X509Certificate2 cert = GetCertificate(credentials.AzureADCertThumbprint);

            config.AddAzureKeyVault(keyVaultEndpoint,
                new ClientCertificateCredential(credentials.AzureADTenantId, credentials.AzureADClientId, cert),
                new KeyVaultSecretManager());
        }

        public static void AddAzureKeyVaultWithClientSecret(this IConfigurationBuilder config, IKeyVaultByClientSecretCredentials credentials)
        {
            Uri keyVaultEndpoint = GetKeyVaultEndpoint(credentials.KeyVaultName);

            var clientSecretCredential = new ClientSecretCredential(credentials.AzureADTenantId, credentials.AzureADClientId, credentials.AzureADClientSecret);
            var client = new SecretClient(keyVaultEndpoint, clientSecretCredential);

            config.AddAzureKeyVault(client, new KeyVaultSecretManager());
        }

        private static X509Certificate2 GetCertificate(string certificateThumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
            X509Certificate2Collection certs = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                certificateThumbprint, false);

            return certs.OfType<X509Certificate2>().Single();
        }

        private static Uri GetKeyVaultEndpoint(string vaultName) => new KeyVaultConfiguration($"https://{vaultName}.vault.azure.net/").VaultUri;
    }
}
