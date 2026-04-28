namespace IdentityService.Contracts.Events;

public sealed record StudentRegisteredEvent(
    int UserId,
    DateTime RegisteredAtUtc,
    int RegisteredYear,
    int RegisteredMonth
);

