using BeerApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace BeerApi.Infrastructure.Data;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
