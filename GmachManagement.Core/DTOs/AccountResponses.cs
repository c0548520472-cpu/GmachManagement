namespace GmachManagement.Core.DTOs;

public sealed record SessionDto(string AccessToken, string TokenType = "Bearer");

public sealed record ProfileUpdateDto(string? AccessToken, string Message);

public sealed record GoogleSignInConfigurationDto(string ClientId, string Nonce);

public sealed record AccountProfileDto(
    string Name, string? Email, bool IsGmachOwner, string? GmachName, string? City, string? GmachType);
