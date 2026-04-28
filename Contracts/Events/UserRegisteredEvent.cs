namespace IdentityService.Contracts.Events;

public sealed record UserRegisteredEvent(
    int UserId,
    string Email,
    IdentityService.Data.Entities.UserRole Role,
    DateTime RegisteredAtUtc
);
