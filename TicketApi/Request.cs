using System;
using System.Collections.Generic;

namespace TicketApi;

public partial class Request
{
    public int RequestId { get; set; }

    public int UserId { get; set; }

    public string ProblemName { get; set; } = null!;

    public string Room { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? Description { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
