using GmachManagement.Core.DTOs;
using GmachManagement.Core.Interfaces;
using GmachManagement.Core.Entities;
namespace GmachManagement.Service.Services;
public sealed class SiteAdminService(ISiteAdminRepository repo) {
 public Task<bool> IsAdminAsync(int id,string? bootstrap,CancellationToken ct)=>repo.IsSiteAdminAsync(id,bootstrap,ct);
 public Task<List<DeletedAccount>> PendingReRegistrationsAsync(CancellationToken ct)=>repo.PendingReRegistrationsAsync(ct);
 public Task<string?> ApproveReRegistrationAsync(string email,CancellationToken ct)=>repo.ApproveReRegistrationAsync(email,ct);
 public Task<string?> RejectReRegistrationAsync(string email,CancellationToken ct)=>repo.RejectReRegistrationAsync(email,ct);
 public Task<List<User>> UsersAsync(CancellationToken ct)=>repo.UsersAsync(ct);
 public Task<List<Gmach>> GmachsAsync(CancellationToken ct)=>repo.GmachsAsync(ct);
 public Task<List<Item>> ItemsAsync(CancellationToken ct)=>repo.ItemsAsync(ct);
 public Task<string?> SetAdminAsync(string email,bool enabled,string? bootstrap,CancellationToken ct)=>repo.SetAdminAsync(email,enabled,bootstrap,ct);
 public Task<string?> ChangeEmailAsync(int id,string email,string? bootstrap,CancellationToken ct)=>repo.ChangeEmailAsync(id,email,bootstrap,ct);
 public Task<string?> EditUserAsync(int id,string name,CancellationToken ct)=>repo.EditUserAsync(id,name,ct);
 public Task<string?> DeleteUserAsync(int id,int actor,string? bootstrap,CancellationToken ct)=>repo.DeleteUserAsync(id,actor,bootstrap,ct);
 public Task<string?> EditGmachAsync(int id,string name,string city,string description,CancellationToken ct)=>repo.EditGmachAsync(id,name,city,description,ct);
 public Task<string?> DeleteGmachAsync(int id,CancellationToken ct)=>repo.DeleteGmachAsync(id,ct);
 public Task<string?> EditItemAsync(int id,string name,string description,CancellationToken ct)=>repo.EditItemAsync(id,name,description,ct);
 public Task<string?> DeleteItemAsync(int id,CancellationToken ct)=>repo.DeleteItemAsync(id,ct);

 public Task<List<OpenLoanDto>> OpenLoansAsync(CancellationToken ct) => repo.OpenLoansAsync(ct);
 public Task<int> DeleteOpenLoansAsync(IReadOnlyCollection<OpenLoanKey> keys, bool all, CancellationToken ct)
     => repo.DeleteOpenLoansAsync(keys, all, ct);
}
