using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken ct = default);
}
