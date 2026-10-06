using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace BeerApi.Infrastructure.Services.Messaging;

public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionProvider(IConfiguration configuration)
    {
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = configuration.GetValue("RabbitMQ:Port", 5672),
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "BeerApi"
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection is not null)
                await _connection.DisposeAsync();

            _connection = await _factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _gate.Dispose();
    }
}