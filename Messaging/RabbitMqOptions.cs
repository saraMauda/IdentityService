using System.ComponentModel.DataAnnotations;

namespace IdentityService.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    [Required]
    public string VirtualHost { get; init; } = "/";

    public string? Username { get; init; }
    public string? Password { get; init; }
}

