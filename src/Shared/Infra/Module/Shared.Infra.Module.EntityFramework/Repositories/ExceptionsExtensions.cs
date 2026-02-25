
using System;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Shared.Infra.Module.EntityFramework.Repositories;

public static class ExceptionsExtensions
{
    public static bool IsPKException(this Exception ex, out string? pkName, out string? message)
    {
        pkName = null;
        message = null;

        if (ex.InnerException is SqlException exSql)
        {
            var matches = Regex.Matches(exSql.Message, "'(.*?)'");
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    if (m.Groups.Count < 2 || pkName != null)
                        break;

                    foreach (Group i in m.Groups)
                    {
                        if (i.Value.StartsWith("pk", StringComparison.OrdinalIgnoreCase) || i.Value.StartsWith("ix", StringComparison.OrdinalIgnoreCase))
                        {
                            pkName = i.Value;
                            break;
                        }
                    }
                }

            }
            
            pkName ??= "[fields]";
            message = exSql.Message;
            return true;
        }

        return false;
    }
}
