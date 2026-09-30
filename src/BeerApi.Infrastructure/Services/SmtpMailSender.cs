using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace BeerApi.Infrastructure.Services;

public sealed class SmtpMailSender(IConfiguration configuration) : IMailSender
{
    public async Task SendAsync(string email, string subject, string body, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            configuration["Smtp:FromName"] ?? "BeerApi",
            configuration["Smtp:FromAddress"] ?? "no-reply@beerapi.local"));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        var secureSocketOptions = configuration.GetValue<bool>("Smtp:UseStartTls")
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;
        await client.ConnectAsync(
            configuration["Smtp:Host"] ?? "localhost",
            configuration.GetValue("Smtp:Port", 1025),
            secureSocketOptions,
            ct);

        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password ?? string.Empty, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}