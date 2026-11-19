using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Repositories;

public sealed class GmachCommunicationRepository(AppDbContext db) : IGmachCommunicationRepository
{
    public Task<List<BorrowApproval>> GetApprovalsAsync(int userId, CancellationToken ct)
        => db.Database.SqlQuery<BorrowApproval>($"""
            SELECT r."Id", i."Name" AS "ItemName", g."Name" AS "GmachName", r."FulfilledAt" AS "ApprovedAt", a."Seen"
            FROM "BorrowRequestApplicants" a
            JOIN "BorrowRequests" r ON r."Id" = a."RequestId"
            JOIN "Items" i ON i."Id" = r."ItemId"
            JOIN "Gmachs" g ON g."Id" = i."GmachId"
            WHERE a."UserId" = {userId} AND r."FulfilledAt" IS NOT NULL
            ORDER BY r."FulfilledAt" DESC, r."Id" DESC
            """).ToListAsync(ct);

    public async Task MarkApprovalSeenAsync(int userId, int requestId, CancellationToken ct)
        => await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "BorrowRequestApplicants" SET "Seen" = TRUE
            WHERE "UserId" = {userId} AND "RequestId" = {requestId}
            """, ct);

    public Task<List<GmachChatThread>> GetOwnerInboxAsync(int ownerId, CancellationToken ct)
        => db.Database.SqlQuery<GmachChatThread>($"""
            SELECT m."GmachId", m."CustomerId", u."Name" AS "CustomerName",
              CAST(COUNT(*) FILTER (WHERE m."SenderId" = m."CustomerId" AND m."ReadByOwnerAt" IS NULL) AS integer) AS "UnreadCount",
              MAX(m."SentAt") AS "LastMessageAt",
              (SELECT LEFT(lastmsg."Text", 140) FROM "GmachChatMessages" lastmsg WHERE lastmsg."GmachId" = m."GmachId" AND lastmsg."CustomerId" = m."CustomerId" ORDER BY lastmsg."Id" DESC LIMIT 1) AS "LastMessagePreview"
            FROM "GmachChatMessages" m
            JOIN "Gmachs" g ON g."Id" = m."GmachId"
            JOIN "Users" u ON u."Id" = m."CustomerId"
            WHERE g."OwnerId" = {ownerId}
            GROUP BY m."GmachId", m."CustomerId", u."Name"
            ORDER BY MAX(m."Id") DESC
            """).ToListAsync(ct);

    public async Task<List<GmachChatMessage>?> GetChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct)
    {
        var owner = await FindOwnerAsync(gmachId, ct);
        if (owner == null || (customerId.HasValue ? owner.Id != actorId : owner.Id == actorId))
            return null;

        var customer = customerId ?? actorId;
        return await db.Database.SqlQuery<GmachChatMessage>($"""
            SELECT "Id", "SenderId", "Text", "SentAt", "ReadByOwnerAt", "ReadByCustomerAt", "AttachmentName"
            FROM "GmachChatMessages"
            WHERE "GmachId" = {gmachId} AND "CustomerId" = {customer}
            """).OrderByDescending(message => message.Id).Take(250).ToListAsync(ct);
    }

    public Task<bool> CanAccessChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct)
        => db.Gmachs.AnyAsync(gmach => gmach.Id == gmachId &&
            (customerId.HasValue ? gmach.OwnerId == actorId && customerId.Value != actorId : gmach.OwnerId != actorId), ct);

    public async Task MarkChatReadByCustomerAsync(int gmachId, int customerId, int throughId, CancellationToken ct)
    {
        var readAt = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "GmachChatMessages" AS m
            SET "ReadByCustomerAt" = {readAt}
            WHERE m."GmachId" = {gmachId} AND m."CustomerId" = {customerId}
              AND m."SenderId" <> {customerId} AND m."Id" <= {throughId} AND m."ReadByCustomerAt" IS NULL
              AND EXISTS (SELECT 1 FROM "Gmachs" g WHERE g."Id" = m."GmachId" AND g."OwnerId" <> {customerId})
            """, ct);
    }

    public async Task MarkChatReadByOwnerAsync(int gmachId, int customerId, int ownerId, int throughId, CancellationToken ct)
    {
        var readAt = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "GmachChatMessages" AS m
            SET "ReadByOwnerAt" = {readAt}
            WHERE m."GmachId" = {gmachId} AND m."CustomerId" = {customerId}
              AND m."SenderId" = {customerId} AND m."Id" <= {throughId} AND m."ReadByOwnerAt" IS NULL
              AND EXISTS (SELECT 1 FROM "Gmachs" g WHERE g."Id" = m."GmachId" AND g."OwnerId" = {ownerId})
            """, ct);
    }

    public Task<ChatAttachment?> GetAttachmentAsync(int gmachId, int messageId, int actorId, CancellationToken ct)
        => db.Database.SqlQuery<ChatAttachment>($"""
            SELECT m."AttachmentName" AS "Name", m."AttachmentContent" AS "Content"
            FROM "GmachChatMessages" m
            JOIN "Gmachs" g ON g."Id" = m."GmachId"
            WHERE m."Id" = {messageId} AND m."GmachId" = {gmachId}
              AND m."AttachmentContent" IS NOT NULL
              AND (g."OwnerId" = {actorId} OR m."CustomerId" = {actorId})
            """).SingleOrDefaultAsync(ct);

    public async Task<(bool Saved, string? NotificationEmail, bool ToCustomer)> AddMessageAsync(
        int gmachId, int? customerId, int actorId, string message, int? itemId, ChatAttachment? attachment, CancellationToken ct)
    {
        var owner = await FindOwnerAsync(gmachId, ct);
        if (owner == null || (customerId.HasValue ? owner.Id != actorId : owner.Id == actorId))
            return (false, null, false);

        var customer = customerId ?? actorId;
        if (customerId.HasValue && !await db.Database.SqlQuery<int>($"""
            SELECT "Id" AS "Value" FROM "GmachChatMessages"
            WHERE "GmachId" = {gmachId} AND "CustomerId" = {customer}
            """).AnyAsync(ct))
            return (false, null, false);

        if (itemId is int id)
        {
            var name = await db.Items.Where(item => item.Id == id && item.GmachId == gmachId && item.IsActive)
                .Select(item => item.Name).SingleOrDefaultAsync(ct);
            if (name == null) return (false, null, false);
            message = $"אני מתעניין בפריט {name}";
        }

        var now = DateTime.UtcNow;
        if (attachment is null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "GmachChatMessages" ("GmachId", "CustomerId", "SenderId", "Text", "SentAt")
                VALUES ({gmachId}, {customer}, {actorId}, {message}, {now})
                """, ct);
        }
        else
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "GmachChatMessages" ("GmachId", "CustomerId", "SenderId", "Text", "SentAt",
                    "AttachmentName", "AttachmentContent")
                VALUES ({gmachId}, {customer}, {actorId}, {message}, {now}, {attachment.Name}, {attachment.Content})
                """, ct);
        }
        return (true, customerId.HasValue ? await db.Users.Where(u=>u.Id==customer).Select(u=>u.Email).SingleOrDefaultAsync(ct) : owner.Email, customerId.HasValue);
    }

    private Task<OwnerContact?> FindOwnerAsync(int gmachId, CancellationToken ct)
        => db.Gmachs.Where(gmach => gmach.Id == gmachId)
            .Select(gmach => new OwnerContact(gmach.OwnerId, gmach.Owner.Email))
            .SingleOrDefaultAsync(ct);

    private sealed record OwnerContact(int Id, string Email);
}
