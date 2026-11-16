namespace GmachManagement.Core.DTOs;

public sealed record OpenLoanDto(string Kind, int Id, string ItemName, string BorrowerName, DateTime BorrowedAt);

public sealed record OpenLoanKey(string Kind, int Id);
