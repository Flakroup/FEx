using FEx.Fundamentals.Extensions;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace FEx.EFCore.Helpers;

public static class SQLConnectionHelper
{
    public static async Task<bool> CheckDbConnectionAsync(string connectionString)
    {
        try
        {
#if NETSTANDARD
            using var connection = new SqlConnection(connectionString);
#else
            await using var connection = new SqlConnection(connectionString);
#endif
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            ex.HandleException();
            return false;
        }
    }

    public static bool CheckDbConnection(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            ex.HandleException();
            return false;
        }
    }
}