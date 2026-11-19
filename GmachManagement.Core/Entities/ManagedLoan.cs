namespace GmachManagement.Core.Entities;
public class ManagedLoan {
 public int Id {get;set;}
 public int GmachId {get;set;}
 public int ItemCopyId {get;set;}
 public string BorrowerName {get;set;} = "";
 public string? BorrowerPhone {get;set;}
 public string? BorrowerEmail {get;set;}
 public DateTime BorrowedAt {get;set;}
 public DateTime? ExpectedReturnDate {get;set;}
 public DateTime? ReturnedAt {get;set;}
}
