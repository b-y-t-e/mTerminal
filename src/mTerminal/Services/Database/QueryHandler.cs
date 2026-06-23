using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace mTerminal.Services.Database;

public sealed class QueryHandler
{
    private readonly IDbProvider _provider;
    private const int MaxRows = 50_000;
    private const int MaxResponseBytes = 16 * 1024 * 1024;

    public QueryHandler(IDbProvider provider)
    {
        _provider = provider;
    }

    public string Execute(string sql, int timeoutSeconds = 120)
    {
        using var conn = _provider.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = timeoutSeconds;
        using var reader = cmd.ExecuteReader();
        return reader.FieldCount > 0
            ? BuildSelectResult(reader)
            : $"{{\"rowsAffected\":{reader.RecordsAffected}}}";
    }

    private static string BuildSelectResult(DbDataReader reader)
    {
        var sb = new StringBuilder();
        var columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            columns[i] = reader.GetName(i);

        sb.Append('[');
        bool firstRow = true;
        int rowCount = 0;
        bool truncated = false;
        while (reader.Read())
        {
            if (++rowCount > MaxRows || sb.Length > MaxResponseBytes)
            {
                truncated = true;
                break;
            }
            if (!firstRow) sb.Append(',');
            firstRow = false;
            sb.Append('{');
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (i > 0) sb.Append(',');
                AppendJsonString(sb, columns[i]);
                sb.Append(':');
                AppendJsonValue(sb, reader, i);
            }
            sb.Append('}');
        }
        sb.Append(']');
        if (truncated)
            throw new InvalidOperationException(
                $"Result truncated: exceeded {(rowCount > MaxRows ? $"{MaxRows} rows" : $"{MaxResponseBytes / 1024 / 1024}MB")} limit. Use TOP/LIMIT or WHERE to narrow your query.");
        return sb.ToString();
    }

    private static void AppendJsonValue(StringBuilder sb, DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) { sb.Append("null"); return; }
        var type = reader.GetFieldType(ordinal);
        if (type == typeof(bool))
            sb.Append(reader.GetBoolean(ordinal) ? "true" : "false");
        else if (type == typeof(short) || type == typeof(int) || type == typeof(long))
            sb.Append(Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture));
        else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            sb.Append(Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture));
        else if (type == typeof(DateTime))
            AppendJsonString(sb, reader.GetDateTime(ordinal).ToString("O"));
        else if (type == typeof(DateTimeOffset))
            AppendJsonString(sb, ((DateTimeOffset)reader.GetValue(ordinal)).ToString("O"));
        else if (type == typeof(DateOnly))
            AppendJsonString(sb, reader.GetValue(ordinal) is DateOnly d ? d.ToString("yyyy-MM-dd") : "");
        else if (type == typeof(TimeOnly))
            AppendJsonString(sb, reader.GetValue(ordinal) is TimeOnly t ? t.ToString("HH:mm:ss.FFFFFFF") : "");
        else if (type == typeof(TimeSpan))
            AppendJsonString(sb, reader.GetValue(ordinal) is TimeSpan ts ? ts.ToString("c") : "");
        else if (type == typeof(Guid))
            AppendJsonString(sb, reader.GetGuid(ordinal).ToString());
        else if (type == typeof(byte[]))
            AppendJsonString(sb, Convert.ToBase64String((byte[])reader.GetValue(ordinal)));
        else
            AppendJsonString(sb, reader.GetValue(ordinal)?.ToString() ?? "");
    }

    private static void AppendJsonString(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}
