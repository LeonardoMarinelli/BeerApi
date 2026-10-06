using AwesomeAssertions;
using BeerApi.Application.Reports;
using BeerApi.Domain.Exceptions;

namespace BeerApi.UnitTests.Services;

public class CsvReportFormatterTests
{
    [Fact]
    public void Write_EscapesSeparatorsAndProtectsFormulaCells()
    {
        var csv = CsvReportFormatter.Write(
        [
            new[] { "beer_name", "amount" },
            new[] { "=HYPERLINK(\"https://invalid\")", "a,b" }
        ]);

        csv.Should().Be("beer_name,amount\r\n\"'=HYPERLINK(\"\"https://invalid\"\")\",\"a,b\"");
    }

    [Fact]
    public void Write_ExceedingDataRowLimit_ThrowsBusinessException()
    {
        var rows = Enumerable.Range(0, 10_002).Select(index => (IReadOnlyList<string>)[index.ToString()]);

        var act = () => CsvReportFormatter.Write(rows);

        act.Should().Throw<BusinessException>();
    }
}