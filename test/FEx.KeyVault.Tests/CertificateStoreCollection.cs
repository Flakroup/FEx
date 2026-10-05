using Xunit;

namespace FEx.KeyVault.Tests;

/// <summary>Tests that add and remove certificates in the current user's store run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CertificateStoreCollection
{
    public const string Name = "FEx.KeyVault certificate store";
}
