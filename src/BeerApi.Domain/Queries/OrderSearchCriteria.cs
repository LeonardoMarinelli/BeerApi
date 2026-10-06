using BeerApi.Domain.Enums;

namespace BeerApi.Domain.Queries;

public sealed record OrderSearchCriteria(int? BreweryId, int? WholesalerId, OrderStatus? Status);