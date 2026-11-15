namespace GmachManagement.Core.Entities;
public class BorrowRequest {
 public int Id {get;set;}
 public int ItemId {get;set;}
 public Item Item {get;set;} = null!;
 public string ApplicantName {get;set;} = "";
 public string? Phone {get;set;}
 public string? Email {get;set;}
 public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
 public DateTime? FulfilledAt {get;set;}
}
