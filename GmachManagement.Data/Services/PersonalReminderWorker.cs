using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using GmachManagement.Data.Context;
using GmachManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GmachManagement.Data.Services;

public sealed class PersonalReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<PersonalReminderWorker> _logger;
    private readonly HashSet<int> _sentToday = new();
    private DateTime _day = DateTime.MinValue;

    public PersonalReminderWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<PersonalReminderWorker> logger)
    { _scopes = scopes; _config = config; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        while (!ct.IsCancellationRequested)
        {
            try { await SendDueAsync(client, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Reminder check failed"); }
            try { await Task.Delay(TimeSpan.FromHours(1), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }

    private async Task SendDueAsync(HttpClient client, CancellationToken ct)
    {
        var apiKey = _config["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;
        var sender = _config["Resend:FromEmail"] ?? "no-reply@gmach-manageonline.online";
        var today = DateTime.UtcNow.Date;
        if (_day != today) { _sentToday.Clear(); _day = today; }
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Send one day before the due date, then each day from the due date until completion.
        var cutoff = today.AddDays(2);
        var due = await db.PersonalTransactions.AsNoTracking().Include(t => t.User)
            .Where(t => !t.IsCompleted && t.ReminderDate != null && t.ReminderDate < cutoff)
            .ToListAsync(ct);
        foreach (var transaction in due)
        {
            if (_sentToday.Contains(transaction.Id) || string.IsNullOrWhiteSpace(transaction.User.Email)) continue;
            if (await db.EmailReminderDeliveries.AnyAsync(x=>x.Kind=="personal" && x.ReferenceId==transaction.Id && x.ReminderDay==today,ct)) continue;
            var date = transaction.ReminderDate!.Value.Date;
            var subject = date > today ? "תזכורת: החזרת פריט מחר" :
                date == today ? "תזכורת: מועד החזרת הפריט היום" : "תזכורת: מועד החזרת הפריט עבר";
            var item = HtmlEncoder.Default.Encode(transaction.ItemDescription ?? "פריט");
            var html = $"<div dir='rtl'><p>תזכורת לגבי {item}.</p><p>מועד ההחזרה: {date:dd/MM/yyyy}</p><p>אם הפריט הוחזר, יש לסמן את הפעולה כהושלמה באתר.</p></div>";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = JsonContent.Create(new { from = sender, to = new[] { transaction.User.Email }, subject, html });
                using var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Resend returned HTTP {(int)response.StatusCode}");
                db.EmailReminderDeliveries.Add(new EmailReminderDelivery { Kind="personal",ReferenceId=transaction.Id,ReminderDay=today,SentAt=DateTime.UtcNow });
                await db.SaveChangesAsync(ct);
                _sentToday.Add(transaction.Id);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            { _logger.LogWarning(ex, "Reminder email failed for transaction {TransactionId}", transaction.Id); }
        }

        var managed = await db.ManagedLoans.AsNoTracking()
            .Where(l => l.ReturnedAt == null && l.ExpectedReturnDate != null &&
                l.ExpectedReturnDate < cutoff && l.BorrowerEmail != null && l.BorrowerEmail != "")
            .ToListAsync(ct);
        foreach (var loan in managed)
        {
            if (await db.EmailReminderDeliveries.AnyAsync(x=>x.Kind=="managed" && x.ReferenceId==loan.Id && x.ReminderDay==today,ct)) continue;
            var date = loan.ExpectedReturnDate!.Value.Date;
            var subject = date > today ? "תזכורת: החזרת פריט מחר" :
                date == today ? "תזכורת: מועד החזרת הפריט היום" : "תזכורת: מועד החזרת הפריט עבר";
            var html = $"<div dir='rtl'><p>תזכורת לגבי פריט שהושאל מהגמ״ח.</p><p>מועד החזרה: {date:dd/MM/yyyy}</p><p>אם הפריט כבר הוחזר, אפשר להתעלם מהודעה זו.</p></div>";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,"https://api.resend.com/emails");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",apiKey);
                request.Content = JsonContent.Create(new { from=sender,to=new[]{loan.BorrowerEmail!},subject,html });
                using var response = await client.SendAsync(request,ct);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Resend returned HTTP {(int)response.StatusCode}");
                db.EmailReminderDeliveries.Add(new EmailReminderDelivery {Kind="managed",ReferenceId=loan.Id,ReminderDay=today,SentAt=DateTime.UtcNow});
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            { _logger.LogWarning(ex,"Managed loan reminder failed for loan {LoanId}",loan.Id); }
        }
    }
}
