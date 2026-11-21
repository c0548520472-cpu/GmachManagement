namespace GmachManagement.Core.DTOs;

public sealed record SiteAdminUserDto(int Id, string Name, string Email, string Role)
{
    public bool IsPrimary { get; init; }
}

public sealed record SiteAdminGmachDto(int Id, string Name, string City, string? Description, int OwnerId);

public sealed record SiteAdminItemDto(int Id, string Name, string Description, int? GmachId, bool IsActive);

public sealed record ReRegistrationRequestDto(string Email, DateTime DeletedAtUtc);

public sealed record OpenLoanDeletionResultDto(int Deleted);

public sealed record ImportResultDto(int Imported);

public sealed record CopyBatchResultDto(int Added);
