using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Repositories;
public sealed class ActivityRepository(AppDbContext db) : IActivityRepository {
 public async Task RecordAsync(int? userId,string sessionId,string eventType,string detail,CancellationToken ct){
  await db.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO ""SiteActivityEvents"" (""UserId"", ""SessionId"", ""EventType"", ""Detail"", ""OccurredAtUtc"")
 VALUES ({userId}, {sessionId}, {eventType}, {detail}, {DateTime.UtcNow})",ct);
 }
 public Task<List<SiteActivityEntry>> RecentAsync(int? userId,string? eventType,int limit,CancellationToken ct){
  var query=db.Database.SqlQuery<SiteActivityEntry>($@"SELECT e.""Id"",e.""UserId"",u.""Name"" AS ""UserName"",u.""Email"" AS ""UserEmail"",
 e.""SessionId"",e.""EventType"",e.""Detail"",e.""OccurredAtUtc""
 FROM ""SiteActivityEvents"" e LEFT JOIN ""Users"" u ON u.""Id""=e.""UserId""
 WHERE ({userId} IS NULL OR e.""UserId""={userId})
 AND ({eventType} IS NULL OR e.""EventType""={eventType})
 ORDER BY e.""OccurredAtUtc"" DESC LIMIT {limit}");
  return query.ToListAsync(ct);
 }
}
