using AwesomeAssertions;
using BeerApi.Application.Services;

namespace BeerApi.UnitTests.Services;

public class VolumeDiscountPolicyTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(10, 0)]
    [InlineData(11, 10)]
    [InlineData(20, 10)]
    [InlineData(21, 20)]
    public void GetPercent_ReturnsDiscountAtQuantityBoundaries(int quantity, decimal expectedPercent)
    {
        VolumeDiscountPolicy.GetPercent(quantity).Should().Be(expectedPercent);
    }
}