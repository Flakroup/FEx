using LiteDB;
using System.IO;

namespace FEx.PersistentStorage.Extensions;

public class LiteRepositoryExtensions
{
    public static LiteRepository GetRepository(string dbFilePath)
    {
        var connectionString = new ConnectionString("Mode=Exclusive")
        {
            Filename = dbFilePath,
            Upgrade = true
        };

        try
        {
            return new(connectionString);
        }
        catch (LiteException) //that's an workaround for LiteDB issue
        {
            File.Delete(dbFilePath);

            return new(connectionString);
        }
    }
}