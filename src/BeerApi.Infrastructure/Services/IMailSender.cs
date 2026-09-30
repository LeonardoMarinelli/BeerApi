namespace BeerApi.Infrastructure.Services;

public interface IMailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken ct = default);
}