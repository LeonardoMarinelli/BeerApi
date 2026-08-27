using AwesomeAssertions;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class AuditLogServiceTests
{
    private readonly IAuditLogRepository _auditLogRepository = Substitute.For<IAuditLogRepository>();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(_auditLogRepository);
    }

    [Fact]
    public async Task GetAllAsync_MapsAuditLogsFromRepository()
    {
        var logs = new List<AuditLog>
        {
            new()
            {
                Id = 1, EntityName = "Beer", EntityId = "1", Action = "Create",
                NewValues = "{}", Timestamp = DateTime.UtcNow, UserId = "u1", UserEmail = "brewer@test.local"
            }
        };
        _auditLogRepository.GetAllAsync(1, 20, null, Arg.Any<CancellationToken>()).Returns((logs, logs.Count));

        var result = await _sut.GetAllAsync(1, 20, null);

        result.Items.Should().ContainSingle().Which.EntityName.Should().Be("Beer");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_WithEntityNameFilter_PassesFilterToRepository()
    {
        _auditLogRepository.GetAllAsync(1, 20, "Sale", Arg.Any<CancellationToken>()).Returns((new List<AuditLog>(), 0));

        await _sut.GetAllAsync(1, 20, "Sale");

        await _auditLogRepository.Received(1).GetAllAsync(1, 20, "Sale", Arg.Any<CancellationToken>());
    }
}
