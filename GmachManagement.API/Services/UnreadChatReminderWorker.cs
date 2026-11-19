using GmachManagement.Service.Services;

namespace GmachManagement.API.Services;

/// <summary>Delivers a single email after an online recipient has left a chat message unread for three hours.</summary>
public sealed class UnreadChatReminderWorker(IServiceScopeFactory scopes, ILogger<UnreadChatReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var chat = scope.ServiceProvider.GetRequiredService<GmachCommunicationService>();
                var email = scope.ServiceProvider.GetRequiredService<ResendEmailService>();
                var reminders = await chat.ClaimDueChatRemindersAsync(stoppingToken);
                foreach (var reminder in reminders)
                {
                    try
                    {
                        await email.SendAsync(reminder.Email, "תזכורת: ממתינה לך הודעה בצ׳אט",
                            "<div dir='rtl'><p>ממתינה לך הודעה בצ׳אט של מרכז ההשאלות כבר שלוש שעות.</p><p><a href='https://gmachmanagement-client.pages.dev/'>לקריאת ההודעה באתר</a></p></div>",
                            stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning(ex, "Unread chat reminder email failed for message {MessageId}", reminder.MessageId);
                        await chat.ReleaseChatReminderAsync(reminder.MessageId, stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Unread chat reminder cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
