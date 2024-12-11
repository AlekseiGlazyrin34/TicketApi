using System;
using System.Collections.Generic;

namespace TicketApi.Models;

public partial class Request
{
    public int RequestId { get; set; }

    public int UserId { get; set; }

    public string ProblemName { get; set; } = null!;

    public string Room { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? Description { get; set; }

    public virtual User User { get; set; } = null!;
}
