using System.ComponentModel;

namespace FEx.AzureStorage;

public enum StorageOperation
{
    None,

    [Description("Downloaded")] Download,

    [Description("Uploaded")] Upload
}