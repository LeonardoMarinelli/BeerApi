namespace BeerApi.Application.Services;

public static class VolumeDiscountPolicy
{
    public static decimal GetPercent(int quantity) => quantity > 20 ? 20m : quantity > 10 ? 10m : 0m;
}