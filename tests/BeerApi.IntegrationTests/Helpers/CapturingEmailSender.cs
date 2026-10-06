using System.Collections.Concurrent;
using BeerApi.Infrastructure.Services;

namespace BeerApi.IntegrationTests.Helpers;

public sealed class CapturingEmailSender : IMailSender
{
    private readonly ConcurrentDictionary<string, (string Subject, string Body)> _messages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(string Subject, string Body)>> _history = new(StringComparer.OrdinalIgnoreCase);

    public Task SendAsync(string email, string subject, string body, CancellationToken ct = default)
    {
        _messages[email] = (subject, body);
        _history.GetOrAdd(email, _ => new ConcurrentQueue<(string Subject, string Body)>()).Enqueue((subject, body));
        return Task.CompletedTask;
    }

    public string GetLastBody(string email) => _messages[email].Body;

    public async Task<(string Subject, string Body)> WaitForSubjectAsync(string email, string subjectPrefix, TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            while (true)
            {
                if (_history.TryGetValue(email, out var messages))
                {
                    var matching = messages.FirstOrDefault(message => message.Subject.StartsWith(subjectPrefix, StringComparison.Ordinal));
                    if (matching.Subject is not null)
                        return matching;
                }

                await Task.Delay(25, timeoutSource.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException($"No email with subject prefix '{subjectPrefix}' was sent to '{email}'.");
        }
    }
}