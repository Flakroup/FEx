using LiteDB;
using System.IO;

namespace FEx.LiteDbx;

public class LiteRepositoryFactory
{
    public static LiteRepository GetRepository(string dbFilePath, bool dropOnException = true)
    {
        var connectionString = new ConnectionString("Mode=Exclusive")
        {
            Filename = dbFilePath,
            Upgrade = true
        };

        try
        {
            return new LiteRepository(connectionString);
        }
        catch (LiteException) when (dropOnException) //that's an workaround for LiteDB issue
        {
            File.Delete(dbFilePath);

            return new LiteRepository(connectionString);
        }
    }
}