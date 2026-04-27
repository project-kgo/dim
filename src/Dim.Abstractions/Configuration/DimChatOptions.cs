using System.ComponentModel.DataAnnotations;

namespace Dim.Abstractions.Configuration;

public sealed class DimChatOptions
{
    public const string SectionName = "DimChat";

    [Required]
    public string EndpointPrefix { get; set; } = "/dim";

    [Required]
    public string HubPath { get; set; } = "/hub";

    public DimChatStorageOptions Storage { get; set; } = new();
}
