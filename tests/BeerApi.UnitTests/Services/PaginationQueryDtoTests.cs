using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using BeerApi.Application.DTOs;

namespace BeerApi.UnitTests.Services;

public class PaginationQueryDtoTests
{
    [Fact]
    public void Page_IsLimitedToTenThousand()
    {
        var maximumPage = new PaginationQueryDto { Page = PaginationQueryDto.MaxPage };
        var aboveMaximumPage = new PaginationQueryDto { Page = PaginationQueryDto.MaxPage + 1 };
        var maximumPageErrors = new List<ValidationResult>();
        var aboveMaximumErrors = new List<ValidationResult>();

        Validator.TryValidateObject(
            maximumPage,
            new ValidationContext(maximumPage),
            maximumPageErrors,
            validateAllProperties: true).Should().BeTrue();
        Validator.TryValidateObject(
            aboveMaximumPage,
            new ValidationContext(aboveMaximumPage),
            aboveMaximumErrors,
            validateAllProperties: true).Should().BeFalse();
    }
}
