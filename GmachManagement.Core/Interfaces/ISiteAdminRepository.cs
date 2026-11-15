using GmachManagement.Core.Entities;
namespace GmachManagement.Core.Interfaces;
public interface ISiteAdminRepository {
 Task<bool> IsSiteAdminAsync(int userId,string? bootstrapEmail,CancellationToken ct);
 Task<List<DeletedAccount>> PendingReRegistrationsAsync(CancellationToken ct);
 Task<string?> ApproveReRegistrationAsync(string email,CancellationToken ct);
 Task<List<User>> UsersAsync(CancellationToken ct);
 Task<List<Gmach>> GmachsAsync(CancellationToken ct);
 Task<List<Item>> ItemsAsync(CancellationToken ct);
 Task<string?> SetAdminAsync(string email,bool enabled,string? bootstrapEmail,CancellationToken ct);
 Task<string?> ChangeEmailAsync(int userId,string email,string? bootstrapEmail,CancellationToken ct);
 Task<string?> EditUserAsync(int userId,string name,CancellationToken ct);
 Task<string?> DeleteUserAsync(int userId,int actorId,string? bootstrapEmail,CancellationToken ct);
 Task<string?> EditGmachAsync(int id,string name,string city,string description,CancellationToken ct);
 Task<string?> DeleteGmachAsync(int id,CancellationToken ct);
 Task<string?> EditItemAsync(int id,string name,string description,CancellationToken ct);
 Task<string?> DeleteItemAsync(int id,CancellationToken ct);
}