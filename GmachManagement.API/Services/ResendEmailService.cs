using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace GmachManagement.API.Services;

public sealed class ResendEmailService(HttpClient client, IConfiguration config)
{
    public async Task SendAsync(string to, string subject, string html, CancellationToken ct)
    {
        var apiKey = config["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Resend API key is not configured.");
        var from = config["Resend:FromEmail"] ?? "no-reply@gmach-manageonline.online";
        var senderName = config["Resend:FromName"] ?? "מרכז ההשאלות";
        if (!from.Contains('<') || !from.Contains('>')) from = $"{senderName} <{from.Trim()}>";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { from, to = new[] { to }, subject, html });
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Resend rejected the email: HTTP {(int)response.StatusCode}");
    }
}
