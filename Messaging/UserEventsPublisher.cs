using IdentityService.Contracts.Events;
using MassTransit;

namespace IdentityService.Messaging;

public interface IUserEventsPublisher
{
    Task PublishUserRegistered(UserRegisteredEvent evt, CancellationToken ct);
    Task PublishStudentRegistered(StudentRegisteredEvent evt, CancellationToken ct);
}

public sealed class UserEventsPublisher : IUserEventsPublisher
{
    private readonly IPublishEndpoint _publish;

    public UserEventsPublisher(IPublishEndpoint publish)
    {
        _publish = publish;
    }

    public Task PublishUserRegistered(UserRegisteredEvent evt, CancellationToken ct)
        => _publish.Publish(evt, ct);

    public Task PublishStudentRegistered(StudentRegisteredEvent evt, CancellationToken ct)
        => _publish.Publish(evt, ct);
}

