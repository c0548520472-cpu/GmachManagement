namespace GmachManagement.Core.Interfaces;
public sealed class SiteActivityEntry {
 public long Id {get;set;}
 public int? UserId {get;set;}
 public string? UserName {get;set;}
 public string? UserEmail {get;set;}
 public string SessionId {get;set;}="";
 public string EventType {get;set;}="";
 public string Detail {get;set;}="";
 public DateTime OccurredAtUtc {get;set;}
}
public interface IActivityRepository {
 Task RecordAsync(int? userId,string sessionId,string eventType,string detail,CancellationToken ct);
 Task<List<SiteActivityEntry>> RecentAsync(int? userId,string? eventType,int limit,CancellationToken ct);
}
