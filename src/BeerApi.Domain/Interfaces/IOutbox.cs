using BeerApi.Domain.Events;

namespace BeerApi.Domain.Interfaces;

public interface IOutbox
{
    Task AddAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}