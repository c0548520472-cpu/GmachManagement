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
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var due = await db.PersonalTransactions.AsNoTracking()
            .Include(t => t.User)
            .Where(t => !t.IsCompleted && t.ReminderDate != null &&
                        t.ReminderDate < tomorrow)
            .ToListAsync(ct);

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
