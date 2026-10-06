using AwesomeAssertions;
using BeerApi.Application.Services;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using BeerApi.Domain.Reports;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class ReportServiceTests
{
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(_repository, 10);
    }

    [Fact]
    public async Task GetSalesAsync_UsesInclusiveUtcDateRangeAndMapsRows()
    {
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);
        var rows = new[] { new SalesReportRow(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 3, 12, 42m) };
        _repository.GetSalesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), false, 4, Arg.Any<CancellationToken>())
            .Returns(rows);

        var result = await _sut.GetSalesAsync(from, to, "day", 4);

        result.Should().ContainSingle().Which.Revenue.Should().Be(42m);
        await _repository.Received(1).GetSalesAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            false,
            4,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSalesAsync_PeriodLongerThan366Days_ThrowsBusinessException()
    {
        var act = () => _sut.GetSalesAsync(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 2), "day", null);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task GetSalesAsync_UnknownGrouping_ThrowsBusinessException()
    {
        var act = () => _sut.GetSalesAsync(null, null, "week", null);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task GetTopBeersAsync_LimitOutsideRange_ThrowsBusinessException()
    {
        var act = () => _sut.GetTopBeersAsync(null, null, 51, null);

        await act.Should().ThrowAsync<BusinessException>();
    }
}