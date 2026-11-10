using System.Net;
using System.Net.Mail;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GmachManagement.Data.Services;

public sealed class PersonalReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PersonalReminderWorker> _logger;
    private readonly HashSet<int> _sent = new();
    private DateTime _lastReminderDay = DateTime.MinValue;

    public PersonalReminderWorker(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<PersonalReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SendDueAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Personal reminder check failed"); }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendDueAsync(CancellationToken ct)
    {
        var host = _config["Email:SmtpHost"];
        var from = _config["Email:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(_config["Email:Username"]) || string.IsNullOrWhiteSpace(_config["Email:Password"])) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = DateTime.UtcNow.Date;
        if (_lastReminderDay != today)
        {
            _sent.Clear();
            _lastReminderDay = today;
        }
        var tomorrow = today.AddDays(1);
        var due = await db.PersonalTransactions.AsNoTracking()
            .Include(t => t.User)
            .Where(t => !t.IsCompleted && t.ReminderDate != null &&
                        t.ReminderDate >= today && t.ReminderDate < tomorrow)
            .ToListAsync(ct);

        // Managed Gmach loans: one day before due date and every day afterwards until returned.
        // Persist the last successful send day so restarts do not resend the same reminder.
        var managedDue = await db.ManagedLoans
            .Where(l => l.ReturnedAt == null && l.ExpectedReturnDate != null &&
                        l.ExpectedReturnDate < tomorrow.AddDays(1) &&
                        (l.LastReminderSentDate == null || l.LastReminderSentDate < today))
            .OrderBy(l => l.Id).Take(200).ToListAsync(ct);
        var copyIds = managedDue.Select(l => l.ItemCopyId).Distinct().ToArray();
        var itemNames = await db.ItemCopies.AsNoTracking()
            .Where(c => copyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Item.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        foreach (var loan in managedDue)
        {
            if (string.IsNullOrWhiteSpace(loan.BorrowerEmail)) continue;
            var dueDay = loan.ExpectedReturnDate!.Value.Date;
            if (today < dueDay.AddDays(-1)) continue;
            var itemName = itemNames.GetValueOrDefault(loan.ItemCopyId) ?? "פריט";
            using var message = new MailMessage(from, loan.BorrowerEmail)
            {
                Subject = today < dueDay ? "תזכורת: מחר מועד החזרת הפריט" : "תזכורת להחזרת פריט",
                Body = "שלום " + loan.BorrowerName + ",\\n" +
                    "תזכורת להחזרת הפריט: " + itemName + "\\n" +
                    "תאריך החזרה: " + dueDay.ToString("dd/MM/yyyy") + "\\n" +
                    "אם כבר החזרת את הפריט, נא לפנות לבעל הגמ״ח לעדכון הסטטוס."
            };
            using var client = new SmtpClient(host, int.TryParse(_config["Email:SmtpPort"], out var smtpPort) ? smtpPort : 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"])
            };
            try
            {
                await client.SendMailAsync(message, ct);
                loan.LastReminderSentDate = today;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed sending managed loan reminder {Id}", loan.Id); }
        }

        foreach (var transaction in due)
        {
            if (_sent.Contains(transaction.Id)) continue;
            using var message = new MailMessage(from, transaction.User.Email)
            {
                Subject = "תזכורת להחזרת פריט",
                Body = "תזכורת לגבי הפריט: " + transaction.ItemDescription +
                       "\nאדם קשור: " + transaction.OtherPartyName
            };
            using var client = new SmtpClient(host, int.TryParse(_config["Email:SmtpPort"], out var port) ? port : 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"])
            };
            try
            {
                await client.SendMailAsync(message, ct);
                _sent.Add(transaction.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending personal reminder for transaction {Id}", transaction.Id);
            }
        }
    }
}
