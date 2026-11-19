namespace GmachManagement.Core.Entities;
public sealed class EmailReminderDelivery
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public int ReferenceId { get; set; }
    public DateTime ReminderDay { get; set; }
    public DateTime SentAt { get; set; }
}
