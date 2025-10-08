using FEx.Agnostics.Abstractions.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEx.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Generate MD5 hash from combined arguments
    /// </summary>
    /// <param name="arguments">Arguments to generate MD5 Hash</param>
    /// <returns>Generated MD5 hash from the combined arguments</returns>
    public static string ComputeMd5HashFromArguments(this object[] arguments)
    {
        if (arguments.IsNullOrEmpty())
            return string.Empty;

        var argumentsStringBuilder = new StringBuilder();

        foreach (object argument in arguments)
        {
            object obj = argument switch
            {
                IEnumerable<int> integers => integers.OrderBy(static x => x),
                IEnumerable<long> longs => longs.OrderBy(static x => x),
                IEnumerable<string> strings => strings.OrderBy(static x => x),
                _ => argument
            };

            argumentsStringBuilder.Append(JsonConvert.SerializeObject(obj));
        }

        return argumentsStringBuilder.ToString().ComputeMd5Hash();
    }
}