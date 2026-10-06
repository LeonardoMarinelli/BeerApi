using System.Globalization;
using System.Text;
using BeerApi.Domain.Exceptions;

namespace BeerApi.Application.Reports;

public static class CsvReportFormatter
{
    private const int MaxDataRows = 10_000;

    public static string Write(IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        var rowCount = 0;
        foreach (var row in rows)
        {
            if (rowCount > MaxDataRows)
                throw new BusinessException($"A exportação CSV é limitada a {MaxDataRows} linhas.");
            if (builder.Length > 0)
                builder.Append("\r\n");
            builder.AppendJoin(',', row.Select(Escape));
            rowCount++;
        }
        return builder.ToString();
    }

    public static string Number<T>(T value) where T : IFormattable =>
        value.ToString(null, CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        var first = value.AsSpan().TrimStart();
        if (first.Length > 0 && first[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = $"'{value}";
        if (value.IndexOfAny([',', '"', '\r', '\n']) >= 0)
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}