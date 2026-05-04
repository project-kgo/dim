using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dim.Abstractions.Messages;

public enum MessageKind : short
{
    Text = 1,
    Image = 2,
    TextImage = 3,
    Video = 4,
    Audio = 5,
    File = 6,
    Location = 7,
    Html = 8,
    Custom = 9,
    System = 100
}

public class MessageContent
{
    public required MessageKind Kind { get; set; }

    public string? Text { get; set; }

    public string? Content { get; set; }

    public string? ResourceUrl { get; set; }

    public string? Extension { get; set; }

}