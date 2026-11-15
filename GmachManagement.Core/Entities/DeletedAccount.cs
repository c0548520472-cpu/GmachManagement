namespace GmachManagement.Core.Entities;
public sealed class DeletedAccount {
 public int Id {get;set;}
 public string Email {get;set;}="";
 public bool Approved {get;set;}
 public bool Requested {get;set;}
 public DateTime DeletedAtUtc {get;set;}=DateTime.UtcNow;
}