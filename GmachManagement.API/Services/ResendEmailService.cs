using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
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
        var siteUrl = config["Site:PublicUrl"] ?? "https://gmachmanagement-client.pages.dev/";
        var safeUrl = HtmlEncoder.Default.Encode(siteUrl);
        var footer = $"""
            <div dir="rtl" style="margin-top:24px;padding-top:16px;border-top:1px solid #d8e7e8;font-family:Arial,sans-serif;color:#24525a">
              <strong>מרכז ההשאלות</strong><br>
              מחברים בין אנשים, משאילים מכל הלב.<br>
              <a href="{safeUrl}" style="color:#087e86">לכניסה לאתר מרכז ההשאלות</a>
            </div>
            """;
        html += footer;
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { from, to = new[] { to }, subject, html });
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Resend rejected the email: HTTP {(int)response.StatusCode}");
    }
}
