using System.Collections.Concurrent;
using BeerApi.Infrastructure.Services;

namespace BeerApi.IntegrationTests.Helpers;

public sealed class CapturingEmailSender : IMailSender
{
    private readonly ConcurrentDictionary<string, (string Subject, string Body)> _messages = new(StringComparer.OrdinalIgnoreCase);

    public Task SendAsync(string email, string subject, string body, CancellationToken ct = default)
    {
        _messages[email] = (subject, body);
        return Task.CompletedTask;
    }

    public string GetLastBody(string email) => _messages[email].Body;
}