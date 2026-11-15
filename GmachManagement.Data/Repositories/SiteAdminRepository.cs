using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Repositories;

public sealed class SiteAdminRepository(AppDbContext db) : ISiteAdminRepository {
 private static string Normalize(string email)=>email.Trim().ToLowerInvariant();
 public Task<bool> IsSiteAdminAsync(int userId,string? bootstrapEmail,CancellationToken ct)=>
  db.Users.AsNoTracking().AnyAsync(u=>u.Id==userId && (u.Role==UserRole.SiteAdmin ||
   (bootstrapEmail!=null && u.Email.ToLower()==Normalize(bootstrapEmail))),ct);
 public Task<List<User>> UsersAsync(CancellationToken ct)=>db.Users.AsNoTracking().OrderBy(u=>u.Id).ToListAsync(ct);
 public Task<List<Gmach>> GmachsAsync(CancellationToken ct)=>db.Gmachs.AsNoTracking().OrderBy(g=>g.Id).ToListAsync(ct);
 public Task<List<Item>> ItemsAsync(CancellationToken ct)=>db.Items.AsNoTracking().OrderBy(i=>i.Id).ToListAsync(ct);
 public async Task<string?> SetAdminAsync(string email,bool enabled,string? bootstrapEmail,CancellationToken ct) {
  var normalized=Normalize(email);
  var user=await db.Users.FirstOrDefaultAsync(u=>u.Email.ToLower()==normalized,ct);
  if(user==null)return "כתובת האימייל אינה רשומה באתר. יש להירשם תחילה.";
  if(bootstrapEmail!=null && normalized==Normalize(bootstrapEmail))
   return "הרשאת המנהלת הראשית מוגנת. שינוי שלה אפשרי רק בהגדרות השרת.";
  if(enabled) user.Role=UserRole.SiteAdmin;
  else {
   if(user.Role!=UserRole.SiteAdmin)return "המשתמש אינו מנהל אתר.";
   user.Role=await db.Gmachs.AnyAsync(g=>g.OwnerId==user.Id,ct)?UserRole.GmachAdmin:UserRole.User;
  }
  await db.SaveChangesAsync(ct);return null;
 }
 public async Task<string?> ChangeEmailAsync(int userId,string email,string? bootstrapEmail,CancellationToken ct) {
  var normalized=Normalize(email);
  var user=await db.Users.FirstOrDefaultAsync(u=>u.Id==userId,ct);
  if(user==null)return "המשתמש לא נמצא.";
  if(bootstrapEmail!=null && Normalize(user.Email)==Normalize(bootstrapEmail))
   return "לא ניתן לשנות כאן את כתובת המנהלת הראשית. יש לעדכן את הגדרת השרת.";
  if(await db.Users.AnyAsync(u=>u.Id!=userId&&u.Email.ToLower()==normalized,ct))
   return "כתובת האימייל כבר קיימת במערכת.";
  user.Email=normalized;
  await db.SaveChangesAsync(ct);return null;
 }
 public async Task<string?> EditUserAsync(int userId,string name,CancellationToken ct) {
  var user=await db.Users.FirstOrDefaultAsync(u=>u.Id==userId,ct);
  if(user==null)return "המשתמש לא נמצא.";
  user.Name=name.Trim();await db.SaveChangesAsync(ct);return null;
 }
 public async Task<string?> DeleteUserAsync(int userId,int actorId,string? bootstrapEmail,CancellationToken ct) {
  var user=await db.Users.FirstOrDefaultAsync(u=>u.Id==userId,ct);
  if(user==null)return "המשתמש לא נמצא.";
  if(userId==actorId || user.Role==UserRole.SiteAdmin ||
   (bootstrapEmail!=null && Normalize(user.Email)==Normalize(bootstrapEmail)))
   return "לא ניתן למחוק חשבון מנהל אתר או את החשבון הנוכחי.";
  if(await db.Gmachs.AnyAsync(g=>g.OwnerId==userId,ct) ||
     await db.Loans.AnyAsync(l=>l.UserId==userId,ct) ||
     await db.PersonalTransactions.AnyAsync(t=>t.UserId==userId,ct))
   return "לא ניתן למחוק משתמש עם גמ״ח, השאלות או פעולות קיימות. יש לטפל בנתונים הקשורים קודם.";
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  var normalized=Normalize(user.Email);
  var tombstone=await db.DeletedAccounts.FirstOrDefaultAsync(x=>x.Email==normalized,ct);
  if(tombstone==null)db.DeletedAccounts.Add(new DeletedAccount{Email=normalized,Approved=false,Requested=false,DeletedAtUtc=DateTime.UtcNow});
  else{tombstone.Approved=false;tombstone.Requested=false;tombstone.DeletedAtUtc=DateTime.UtcNow;}
  db.Users.Remove(user);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return null;
 }
 public async Task<string?> EditGmachAsync(int id,string name,string city,string description,CancellationToken ct) {
  var g=await db.Gmachs.FirstOrDefaultAsync(x=>x.Id==id,ct);
  if(g==null)return "הגמ״ח לא נמצא.";
  g.Name=name.Trim();g.City=city.Trim();g.Description=description.Trim();
  await db.SaveChangesAsync(ct);return null;
 }
 public async Task<string?> DeleteGmachAsync(int id,CancellationToken ct) {
  var g=await db.Gmachs.FirstOrDefaultAsync(x=>x.Id==id,ct);
  if(g==null)return "הגמ״ח לא נמצא.";
  if(await db.Items.AnyAsync(i=>i.GmachId==id,ct) || await db.ManagedLoans.AnyAsync(l=>l.GmachId==id,ct))
   return "לא ניתן למחוק גמ״ח עם פריטים או השאלות קיימות.";
  db.Gmachs.Remove(g);await db.SaveChangesAsync(ct);return null;
 }
 public async Task<string?> EditItemAsync(int id,string name,string description,CancellationToken ct) {
  var item=await db.Items.FirstOrDefaultAsync(x=>x.Id==id,ct);
  if(item==null)return "הפריט לא נמצא.";
  item.Name=name.Trim();item.Description=description.Trim();
  await db.SaveChangesAsync(ct);return null;
 }
 public async Task<string?> DeleteItemAsync(int id,CancellationToken ct) {
  var item=await db.Items.FirstOrDefaultAsync(x=>x.Id==id,ct);
  if(item==null)return "הפריט לא נמצא.";
  if(await db.ItemCopies.AnyAsync(c=>c.ItemId==id,ct) || await db.BorrowRequests.AnyAsync(r=>r.ItemId==id,ct))
   { item.IsActive=false; await db.SaveChangesAsync(ct); return null; }
  db.Items.Remove(item);await db.SaveChangesAsync(ct);return null;
 }
}